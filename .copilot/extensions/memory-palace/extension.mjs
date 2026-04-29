import { joinSession } from "@github/copilot-sdk/extension";

const PALACE_MAP = `## MemPalace — Palace Structure

### Wings & Rooms

**🏛️ wing_escrowapp** — Project-specific knowledge
- \`room_architecture\` — Clean Architecture, CQRS, layer decisions
- \`room_payments\` — Stripe integration, hold/release/cancel flows
- \`room_debugging\` — Bug fixes, root causes, error resolutions
- \`room_security\` — OWASP findings, auth patterns, vulnerability fixes
- \`room_decisions\` — ADRs, trade-offs, why-we-chose-X
- \`room_regulatory\` — Compliance rules, terminology, legal constraints

**🧠 wing_nexsynapse** — AI infrastructure knowledge
- \`room_skills\` — Skill authoring patterns, catalog organization
- \`room_extensions\` — Copilot CLI extension patterns, SDK usage
- \`room_agents\` — Custom agent configs, prompt engineering
- \`room_bridges\` — Cross-model bridge file patterns

**📚 wing_dotnet** — .NET / Blazor patterns
- \`room_blazor\` — Component patterns, lifecycle, SSR
- \`room_efcore\` — EF Core queries, migrations, DbContext
- \`room_mediatr\` — Handler patterns, pipeline behaviors

### Recommended Workflow
1. **Before starting work** → \`mempalace_search\` for prior knowledge
2. **After solving a problem** → \`mempalace_add_drawer\` to save the insight
3. **When exploring** → \`mempalace_browse_palace\` to see what's stored`;

const INSIGHT_TEMPLATES = {
    decision: { wing: "wing_escrowapp", room: "room_decisions", icon: "📋" },
    pattern: { wing: "wing_dotnet", room: "room_blazor", icon: "🔧" },
    debug: { wing: "wing_escrowapp", room: "room_debugging", icon: "🐛" },
    security: { wing: "wing_escrowapp", room: "room_security", icon: "🔒" },
    regulatory: { wing: "wing_escrowapp", room: "room_regulatory", icon: "⚖️" },
};

const KEYWORD_ROOMS = [
    {
        keywords: ["debug", "error", "fix", "broken", "crash", "bug", "exception", "stacktrace"],
        suggestion: "MemPalace: Search `room_debugging` in `wing_escrowapp` for prior fixes — use `mempalace_search` with relevant error terms.",
    },
    {
        keywords: ["architecture", "design", "pattern", "refactor", "layer", "structure", "clean architecture"],
        suggestion: "MemPalace: Search `room_architecture` and `room_decisions` for prior architectural decisions — use `mempalace_search`.",
    },
    {
        keywords: ["stripe", "payment", "hold", "release", "capture", "payout", "paymentintent"],
        suggestion: "MemPalace: Search `room_payments` for prior Stripe integration knowledge — use `mempalace_search`.",
    },
    {
        keywords: ["security", "owasp", "auth", "authorize", "vulnerability", "injection", "xss"],
        suggestion: "MemPalace: Search `room_security` for prior security findings — use `mempalace_search`.",
    },
    {
        keywords: ["compliance", "regulatory", "escrow", "legal", "terminology", "money transmitter"],
        suggestion: "MemPalace: Search `room_regulatory` for compliance rules and approved terminology — use `mempalace_search`.",
    },
    {
        keywords: ["skill", "extension", "agent", "bridge", "nexsynapse", "mcp", "copilot sdk"],
        suggestion: "MemPalace: Search `wing_nexsynapse` for AI infrastructure patterns — use `mempalace_search`.",
    },
];

function detectRelevantRoom(prompt) {
    const lower = prompt.toLowerCase();
    for (const entry of KEYWORD_ROOMS) {
        if (entry.keywords.some((kw) => lower.includes(kw))) {
            return entry.suggestion;
        }
    }
    return null;
}

const session = await joinSession({
    hooks: {
        onSessionStart: async () => {
            await session.log("🧠 Memory Palace extension loaded", { ephemeral: true });
            return {
                additionalContext:
                    "MemPalace cross-session memory is available. Use `mempalace_search` to recall relevant past knowledge before starting work. Use `mempalace_add_drawer` to save important decisions, debugging insights, and architectural patterns. Palace structure: wing_escrowapp (rooms: architecture, payments, debugging, security, decisions, regulatory), wing_nexsynapse (rooms: skills, extensions, agents, bridges), wing_dotnet (rooms: blazor, efcore, mediatr).",
            };
        },

        onUserPromptSubmitted: async (input) => {
            const prompt = input.prompt;
            if (!prompt || typeof prompt !== "string") return;

            const suggestion = detectRelevantRoom(prompt);
            if (suggestion) {
                return { additionalContext: suggestion };
            }
        },
    },

    tools: [
        {
            name: "palace_status",
            description:
                "Returns a formatted summary of the MemPalace structure — available wings, rooms, and recommended usage for common tasks. Use this to orient yourself in the memory palace.",
            parameters: {
                type: "object",
                properties: {},
                additionalProperties: false,
            },
            handler: async () => {
                return PALACE_MAP;
            },
        },
        {
            name: "save_insight",
            description:
                "Formats a learning, decision, or debugging insight for palace storage. Returns the formatted content with the target wing/room — then call `mempalace_add_drawer` with the returned values to persist it.",
            parameters: {
                type: "object",
                properties: {
                    type: {
                        type: "string",
                        enum: ["decision", "pattern", "debug", "security", "regulatory"],
                        description: "Category of insight: decision (ADR/trade-off), pattern (reusable code pattern), debug (bug fix/root cause), security (OWASP finding), regulatory (compliance rule).",
                    },
                    title: {
                        type: "string",
                        description: "Short descriptive title for the insight (e.g., 'Stripe idempotency key format').",
                    },
                    content: {
                        type: "string",
                        description: "The insight content — what was learned, decided, or fixed.",
                    },
                },
                required: ["type", "title", "content"],
                additionalProperties: false,
            },
            handler: async ({ type, title, content }) => {
                const template = INSIGHT_TEMPLATES[type];
                if (!template) {
                    return `Error: Unknown insight type "${type}". Use one of: decision, pattern, debug, security, regulatory.`;
                }

                const timestamp = new Date().toISOString().split("T")[0];

                return `${template.icon} **Insight formatted for MemPalace storage**

**Target:** \`${template.wing}\` → \`${template.room}\`
**Drawer title:** ${title}
**Date:** ${timestamp}

**Content to store:**
${content}

---
**Next step:** Call \`mempalace_add_drawer\` with:
- \`wing\`: \`${template.wing}\`
- \`room\`: \`${template.room}\`
- \`drawer_name\`: \`${title.toLowerCase().replace(/[^a-z0-9]+/g, "-").replace(/(^-|-$)/g, "")}\`
- \`content\`: The insight content above`;
            },
        },
    ],
});
