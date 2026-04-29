// NexSynapse Session Tracker Extension
// Persistent cross-session task tracking with zero API cost.
// Reads/writes a local JSON file — no external calls.

import { joinSession } from "@github/copilot-sdk/extension";
import { readFile, writeFile } from 'node:fs/promises';
import { resolve, join } from 'node:path';

const ROOT = resolve('.');
const TRACKER_PATH = join(ROOT, 'NexSynapse', 'state', 'session-tracker.json');

async function loadTracker() {
  try {
    const raw = await readFile(TRACKER_PATH, 'utf-8');
    return JSON.parse(raw);
  } catch {
    return { version: 1, lastUpdated: new Date().toISOString(), items: [], archive: [] };
  }
}

async function saveTracker(data) {
  data.lastUpdated = new Date().toISOString();
  await writeFile(TRACKER_PATH, JSON.stringify(data, null, 2), 'utf-8');
}

function priorityEmoji(p) {
  return { high: '🔴', medium: '🟡', low: '🟢' }[p] || '⚪';
}

function formatStatus(tracker) {
  const pending = tracker.items.filter(i => i.status === 'pending');
  const blocked = tracker.items.filter(i => i.status === 'blocked');
  const inProgress = tracker.items.filter(i => i.status === 'in_progress');

  const lines = [];
  lines.push(`# 📋 Session Tracker`);
  lines.push(`**Last updated:** ${tracker.lastUpdated}`);
  lines.push(`**Pending:** ${pending.length} | **Blocked:** ${blocked.length} | **In Progress:** ${inProgress.length} | **Archived:** ${tracker.archive.length}`);
  lines.push('');

  if (inProgress.length > 0) {
    lines.push('## 🔄 In Progress');
    for (const item of inProgress) {
      lines.push(`- ${priorityEmoji(item.priority)} **${item.title}** (${item.id})`);
      if (item.nextAction) lines.push(`  → Next: ${item.nextAction}`);
    }
    lines.push('');
  }

  if (pending.length > 0) {
    lines.push('## ⏳ Pending');
    const sorted = [...pending].sort((a, b) => {
      const order = { high: 0, medium: 1, low: 2 };
      return (order[a.priority] ?? 3) - (order[b.priority] ?? 3);
    });
    for (const item of sorted) {
      lines.push(`- ${priorityEmoji(item.priority)} **${item.title}** (${item.id})`);
      if (item.nextAction) lines.push(`  → Next: ${item.nextAction}`);
    }
    lines.push('');
  }

  if (blocked.length > 0) {
    lines.push('## 🚫 Blocked');
    for (const item of blocked) {
      lines.push(`- ${priorityEmoji(item.priority)} **${item.title}** (${item.id})`);
      if (item.blockedReason) lines.push(`  ⛔ Reason: ${item.blockedReason}`);
      if (item.nextAction) lines.push(`  → Next: ${item.nextAction}`);
    }
    lines.push('');
  }

  if (pending.length === 0 && blocked.length === 0 && inProgress.length === 0) {
    lines.push('## ✅ All clear — no pending tasks!');
  }

  return lines.join('\n');
}

joinSession(({ onToolCall }) => {

  onToolCall("session_tracker_status", {
    description: "Returns pending/blocked/in-progress tasks from the persistent session tracker. Call at session start to see what needs attention. Zero API cost — reads local JSON only.",
    parameters: {},
    handler: async () => {
      const tracker = await loadTracker();
      return formatStatus(tracker);
    }
  });

  onToolCall("session_tracker_add", {
    description: "Add a new task to the persistent session tracker. Use when new work items, ideas, or follow-ups arise during a session.",
    parameters: {
      id: { type: "string", description: "Kebab-case unique ID (e.g., 'implement-refund-flow')" },
      title: { type: "string", description: "Short descriptive title" },
      priority: { type: "string", description: "Priority level: high, medium, or low" },
      nextAction: { type: "string", description: "Concrete next step to take" },
      status: { type: "string", description: "Initial status: pending, blocked, or in_progress. Default: pending" },
      blockedReason: { type: "string", description: "Why this item is blocked (only if status is blocked)" }
    },
    handler: async ({ id, title, priority = "medium", nextAction = "", status = "pending", blockedReason = null }) => {
      const tracker = await loadTracker();
      if (tracker.items.find(i => i.id === id)) {
        return `❌ Item '${id}' already exists. Use session_tracker_update to modify it.`;
      }
      tracker.items.push({
        id,
        title,
        status,
        priority,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
        sourceSession: process.env.COPILOT_SESSION_ID || "unknown",
        blockedReason: status === 'blocked' ? blockedReason : null,
        nextAction,
        completedAt: null
      });
      await saveTracker(tracker);
      return `✅ Added: ${priorityEmoji(priority)} **${title}** (${id}) — status: ${status}`;
    }
  });

  onToolCall("session_tracker_update", {
    description: "Update an existing task in the session tracker. Use to change status, priority, next action, or mark as done. When status is 'done', the item is moved to the archive automatically.",
    parameters: {
      id: { type: "string", description: "The task ID to update" },
      status: { type: "string", description: "New status: pending, in_progress, blocked, or done" },
      priority: { type: "string", description: "New priority: high, medium, or low" },
      nextAction: { type: "string", description: "Updated next action" },
      blockedReason: { type: "string", description: "Why this item is blocked (only if status is blocked)" }
    },
    handler: async ({ id, status, priority, nextAction, blockedReason }) => {
      const tracker = await loadTracker();
      const idx = tracker.items.findIndex(i => i.id === id);
      if (idx === -1) return `❌ Item '${id}' not found.`;

      const item = tracker.items[idx];
      if (status) item.status = status;
      if (priority) item.priority = priority;
      if (nextAction !== undefined) item.nextAction = nextAction;
      if (blockedReason !== undefined) item.blockedReason = blockedReason;
      item.updatedAt = new Date().toISOString();

      if (status === 'done') {
        item.completedAt = new Date().toISOString();
        tracker.items.splice(idx, 1);
        tracker.archive.push(item);
        await saveTracker(tracker);
        return `✅ Completed and archived: **${item.title}** (${id})`;
      }

      await saveTracker(tracker);
      return `✅ Updated: ${priorityEmoji(item.priority)} **${item.title}** (${id}) — status: ${item.status}`;
    }
  });

});
