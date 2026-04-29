// Extension: copilot-skills
// Generic skill loader for ~/.copilot/skills/ — auto-discovers all installed skills.
// Each skill folder must have SKILL.md; optional references/ subdirectory.
// Provides: copilot_skills_catalog (list all), copilot_skill (load one + optional ref).

import { joinSession } from "@github/copilot-sdk/extension";
import { readFile, readdir, access, stat } from "node:fs/promises";
import { join } from "node:path";

const SKILLS_DIR = join(
  process.env.USERPROFILE || process.env.HOME || "~",
  ".copilot", "skills"
);

async function exists(p) {
  try { await access(p); return true; } catch { return false; }
}

async function discoverSkills() {
  const skills = {};
  let entries;
  try { entries = await readdir(SKILLS_DIR, { withFileTypes: true }); } catch { return skills; }

  for (const entry of entries) {
    if (!entry.isDirectory() && !entry.isSymbolicLink()) continue;
    // Junctions on Windows appear as directories
    const skillDir = join(SKILLS_DIR, entry.name);
    const skillFile = join(skillDir, "SKILL.md");
    if (!(await exists(skillFile))) continue;

    // Parse frontmatter for description
    const raw = await readFile(skillFile, "utf-8");
    const fmMatch = raw.match(/^---\s*\n([\s\S]*?)\n---/);
    let description = entry.name;
    if (fmMatch) {
      const descLine = fmMatch[1].match(/description:\s*(.+)/);
      if (descLine) description = descLine[1].trim();
    }

    // Discover references
    const refs = {};
    const refsDir = join(skillDir, "references");
    if (await exists(refsDir)) {
      const refFiles = await readdir(refsDir);
      for (const rf of refFiles) {
        if (rf.endsWith(".md")) {
          const id = rf.replace(/\.md$/, "");
          refs[id] = join("references", rf);
        }
      }
    }

    skills[entry.name] = { dir: skillDir, description, refs };
  }
  return skills;
}

// Discover once at startup — reload via /clear or extensions_reload
const SKILLS = await discoverSkills();
const skillNames = Object.keys(SKILLS);

const session = await joinSession({
  tools: [
    {
      name: "copilot_skills_catalog",
      description:
        "List all globally installed Copilot skills with descriptions and available references. " +
        "Use this to discover what skills are available before loading one.",
      parameters: { type: "object", properties: {} },
      skipPermission: true,
      handler: async () => {
        if (skillNames.length === 0) {
          return "No skills installed. Add skill folders to ~/.copilot/skills/ (each with a SKILL.md).";
        }
        const lines = skillNames.map((name) => {
          const s = SKILLS[name];
          const refList = Object.keys(s.refs);
          const refStr = refList.length > 0 ? ` | refs: ${refList.join(", ")}` : "";
          return `• **${name}** — ${s.description}${refStr}`;
        });
        return `## Global Copilot Skills (${skillNames.length})\n\n${lines.join("\n")}\n\nLoad a skill: copilot_skill({ skill: "name" })\nLoad a reference: copilot_skill({ skill: "name", ref: "reference-id" })`;
      },
    },
    {
      name: "copilot_skill",
      description:
        "Load a globally installed Copilot skill or one of its reference files. " +
        "Available skills: " + (skillNames.join(", ") || "none") + ". " +
        "Pass just 'skill' for the core methodology, add 'ref' for a specific deep-dive reference.",
      parameters: {
        type: "object",
        properties: {
          skill: {
            type: "string",
            description: "Skill name to load",
            ...(skillNames.length > 0 ? { enum: skillNames } : {}),
          },
          ref: {
            type: "string",
            description: "Optional: specific reference file to load instead of the core skill",
          },
        },
        required: ["skill"],
      },
      skipPermission: true,
      handler: async ({ skill, ref }) => {
        const s = SKILLS[skill];
        if (!s) {
          return `Unknown skill '${skill}'. Available: ${skillNames.join(", ") || "none"}`;
        }

        if (ref) {
          const refPath = s.refs[ref];
          if (!refPath) {
            const available = Object.keys(s.refs);
            return `Unknown reference '${ref}' for skill '${skill}'. Available: ${available.join(", ") || "none"}`;
          }
          return readFile(join(s.dir, refPath), "utf-8");
        }

        // Load core skill + append reference guide
        const core = await readFile(join(s.dir, "SKILL.md"), "utf-8");
        const refKeys = Object.keys(s.refs);
        if (refKeys.length === 0) return core;

        const refGuide =
          "\n\n## Available References (load via copilot_skill with ref parameter)\n\n" +
          refKeys.map((r) => `- **${r}**`).join("\n");
        return core + refGuide;
      },
    },
  ],
});
