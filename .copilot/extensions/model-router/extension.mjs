// Model Router Extension — Copilot CLI
// Intelligent LLM model selection tools for NexSynapse portable AI infrastructure.
// Tools: suggest_model, log_routing_decision, mine_routing_history

import { joinSession } from "@github/copilot-sdk/extension";

// --- Model Data (parsed from matrix for quick scoring) ---

const MODELS = {
  "claude-opus-4.6":    { provider: "Anthropic", tier: "Premium",   scores: { reasoning: 5, code_gen: 5, code_read: 5, instruct: 5, speed: 2, cost: 1, context: 5, tools: 5, creative: 5, consistent: 5 }, output_price: 25.00 },
  "claude-opus-4.5":    { provider: "Anthropic", tier: "Premium",   scores: { reasoning: 5, code_gen: 5, code_read: 5, instruct: 5, speed: 2, cost: 1, context: 5, tools: 5, creative: 5, consistent: 5 }, output_price: 25.00 },
  "claude-sonnet-4.6":  { provider: "Anthropic", tier: "Standard",  scores: { reasoning: 4, code_gen: 5, code_read: 4, instruct: 4, speed: 3, cost: 3, context: 4, tools: 5, creative: 4, consistent: 4 }, output_price: 15.00 },
  "claude-sonnet-4.5":  { provider: "Anthropic", tier: "Standard",  scores: { reasoning: 4, code_gen: 4, code_read: 4, instruct: 4, speed: 3, cost: 3, context: 4, tools: 4, creative: 4, consistent: 4 }, output_price: 15.00 },
  "claude-sonnet-4":    { provider: "Anthropic", tier: "Standard",  scores: { reasoning: 4, code_gen: 4, code_read: 4, instruct: 4, speed: 3, cost: 3, context: 4, tools: 4, creative: 4, consistent: 4 }, output_price: 15.00 },
  "claude-haiku-4.5":   { provider: "Anthropic", tier: "Fast",      scores: { reasoning: 3, code_gen: 3, code_read: 3, instruct: 3, speed: 5, cost: 4, context: 3, tools: 3, creative: 2, consistent: 3 }, output_price: 5.00 },
  "gpt-5.4":            { provider: "OpenAI",    tier: "Standard",  scores: { reasoning: 4, code_gen: 4, code_read: 4, instruct: 4, speed: 3, cost: 3, context: 4, tools: 4, creative: 4, consistent: 4 }, output_price: 15.00 },
  "gpt-5.3-codex":      { provider: "OpenAI",    tier: "Standard",  scores: { reasoning: 4, code_gen: 5, code_read: 4, instruct: 4, speed: 3, cost: 3, context: 4, tools: 4, creative: 3, consistent: 4 }, output_price: 15.00 },
  "gpt-5.2-codex":      { provider: "OpenAI",    tier: "Standard",  scores: { reasoning: 4, code_gen: 5, code_read: 4, instruct: 4, speed: 3, cost: 3, context: 4, tools: 4, creative: 3, consistent: 4 }, output_price: 15.00 },
  "gpt-5.2":            { provider: "OpenAI",    tier: "Standard",  scores: { reasoning: 4, code_gen: 4, code_read: 4, instruct: 4, speed: 3, cost: 3, context: 4, tools: 4, creative: 4, consistent: 4 }, output_price: 15.00 },
  "gpt-5.1":            { provider: "OpenAI",    tier: "Standard",  scores: { reasoning: 4, code_gen: 4, code_read: 4, instruct: 4, speed: 3, cost: 3, context: 4, tools: 4, creative: 3, consistent: 4 }, output_price: 15.00 },
  "gpt-5.4-mini":       { provider: "OpenAI",    tier: "Fast",      scores: { reasoning: 3, code_gen: 3, code_read: 3, instruct: 3, speed: 5, cost: 5, context: 3, tools: 3, creative: 2, consistent: 3 }, output_price: 2.00 },
  "gpt-5-mini":         { provider: "OpenAI",    tier: "Fast",      scores: { reasoning: 3, code_gen: 3, code_read: 2, instruct: 3, speed: 5, cost: 5, context: 3, tools: 3, creative: 2, consistent: 3 }, output_price: 2.00 },
  "gpt-4.1":            { provider: "OpenAI",    tier: "Fast",      scores: { reasoning: 3, code_gen: 3, code_read: 3, instruct: 3, speed: 4, cost: 4, context: 3, tools: 3, creative: 3, consistent: 3 }, output_price: 8.00 },
  "gemini-2.5-pro":     { provider: "Google",    tier: "Standard",  scores: { reasoning: 4, code_gen: 4, code_read: 4, instruct: 4, speed: 3, cost: 4, context: 5, tools: 4, creative: 4, consistent: 3 }, output_price: 10.00 },
  "gemini-2.5-flash":   { provider: "Google",    tier: "Fast",      scores: { reasoning: 3, code_gen: 3, code_read: 3, instruct: 3, speed: 5, cost: 5, context: 4, tools: 3, creative: 2, consistent: 3 }, output_price: 2.50 },
};

// --- Weight Profiles (per task category) ---

const WEIGHT_PROFILES = {
  "code-review":            { reasoning: 4, code_gen: 2, code_read: 5, instruct: 3, speed: 3, cost: 3, context: 3, tools: 2, creative: 2, consistent: 4 },
  "security-audit":         { reasoning: 5, code_gen: 2, code_read: 5, instruct: 4, speed: 2, cost: 2, context: 4, tools: 3, creative: 3, consistent: 4 },
  "test-generation":        { reasoning: 3, code_gen: 5, code_read: 3, instruct: 4, speed: 3, cost: 3, context: 3, tools: 2, creative: 2, consistent: 4 },
  "complex-implementation": { reasoning: 5, code_gen: 5, code_read: 4, instruct: 4, speed: 2, cost: 2, context: 4, tools: 4, creative: 4, consistent: 4 },
  "refactoring":            { reasoning: 4, code_gen: 4, code_read: 5, instruct: 3, speed: 2, cost: 3, context: 3, tools: 3, creative: 3, consistent: 4 },
  "debugging":              { reasoning: 5, code_gen: 3, code_read: 5, instruct: 3, speed: 2, cost: 2, context: 4, tools: 4, creative: 4, consistent: 3 },
  "documentation":          { reasoning: 2, code_gen: 2, code_read: 3, instruct: 4, speed: 4, cost: 4, context: 2, tools: 1, creative: 2, consistent: 3 },
  "architecture-review":    { reasoning: 5, code_gen: 2, code_read: 4, instruct: 3, speed: 2, cost: 2, context: 5, tools: 3, creative: 4, consistent: 4 },
  "code-exploration":       { reasoning: 2, code_gen: 1, code_read: 3, instruct: 2, speed: 5, cost: 5, context: 2, tools: 4, creative: 1, consistent: 3 },
  "build-test-execution":   { reasoning: 1, code_gen: 1, code_read: 1, instruct: 2, speed: 5, cost: 5, context: 1, tools: 4, creative: 1, consistent: 3 },
  "planning-decomposition": { reasoning: 5, code_gen: 2, code_read: 3, instruct: 4, speed: 2, cost: 3, context: 4, tools: 2, creative: 4, consistent: 3 },
  "prompt-engineering":     { reasoning: 4, code_gen: 2, code_read: 3, instruct: 5, speed: 2, cost: 3, context: 3, tools: 2, creative: 5, consistent: 3 },
};

const COMPLEXITY_ADJUSTMENTS = {
  S:  { reasoning: -2, speed: 2, cost: 2 },
  M:  {},
  L:  { reasoning: 1, context: 1, speed: -1 },
  XL: { reasoning: 2, context: 2, creative: 1, speed: -2, cost: -2 },
};

function clamp(val, min, max) { return Math.max(min, Math.min(max, val)); }

function scoreModel(modelId, category, complexity, budgetMode) {
  const model = MODELS[modelId];
  if (!model) return null;

  const baseWeights = WEIGHT_PROFILES[category];
  if (!baseWeights) return null;

  const adj = COMPLEXITY_ADJUSTMENTS[complexity] || {};
  const weights = {};
  for (const dim of Object.keys(baseWeights)) {
    weights[dim] = clamp((baseWeights[dim] || 0) + (adj[dim] || 0), 0, 5);
  }

  let rawScore = 0;
  for (const dim of Object.keys(weights)) {
    rawScore += weights[dim] * (model.scores[dim] || 0);
  }

  let costFactor;
  switch (budgetMode) {
    case "cost-optimized": costFactor = model.output_price / 2; break;
    case "quality-first":  costFactor = 1; break;
    default:               costFactor = model.output_price / 10; break;
  }

  return { modelId, score: rawScore / Math.max(costFactor, 0.1), rawScore, costFactor, tier: model.tier, provider: model.provider, outputPrice: model.output_price };
}

function suggestModel(task, category, complexity = "M", budgetMode = "balanced", constraints = {}) {
  const results = [];

  for (const modelId of Object.keys(MODELS)) {
    const model = MODELS[modelId];

    // Apply tier filter
    if (budgetMode === "cost-optimized" && model.tier === "Premium") continue;

    // Apply domain sensitivity filter
    if (constraints.domainSensitive && model.scores.reasoning < 4) continue;

    // Apply code modification filter
    if (constraints.writesCode && model.scores.code_gen < 4) continue;

    const result = scoreModel(modelId, category, complexity, budgetMode);
    if (result) results.push(result);
  }

  results.sort((a, b) => b.score - a.score);

  return {
    recommended: results[0] || null,
    fallback: results[1] || null,
    allScores: results.slice(0, 5),
  };
}

// --- Extension Registration ---

const session = await joinSession({
  tools: [
    {
      name: "suggest_model",
      description: "Score all available models for a task and recommend the optimal one. Returns primary recommendation + fallback + top 5 scores.",
      parameters: {
        type: "object",
        properties: {
          task: { type: "string", description: "Brief description of what the model will do" },
          category: {
            type: "string",
            description: "Task category",
            enum: Object.keys(WEIGHT_PROFILES),
          },
          complexity: {
            type: "string",
            description: "Task complexity: S (simple), M (medium), L (large), XL (extra large)",
            enum: ["S", "M", "L", "XL"],
          },
          budget: {
            type: "string",
            description: "Budget preference",
            enum: ["cost-optimized", "balanced", "quality-first"],
          },
          writes_code: { type: "boolean", description: "Whether the agent will modify code" },
          domain_sensitive: { type: "boolean", description: "Whether the task involves fintech, security, or compliance" },
          parallel_fleet: { type: "boolean", description: "Whether this is one of many agents in a parallel fleet" },
        },
        required: ["task", "category"],
      },
      handler: async (args) => {
        const effectiveBudget = args.parallel_fleet ? "cost-optimized" : (args.budget || "balanced");
        const constraints = {
          writesCode: args.writes_code || false,
          domainSensitive: args.domain_sensitive || false,
        };

        const result = suggestModel(args.task, args.category, args.complexity || "M", effectiveBudget, constraints);

        if (!result.recommended) {
          return `No eligible models found for category "${args.category}" with given constraints.`;
        }

        const rec = result.recommended;
        const fb = result.fallback;

        let output = `📊 Model Recommendation\n`;
        output += `Task: ${args.task}\n`;
        output += `Category: ${args.category} | Complexity: ${args.complexity || "M"}\n`;
        output += `Budget: ${effectiveBudget}${args.parallel_fleet ? " (auto: parallel fleet)" : ""}\n\n`;
        output += `✅ Recommended: ${rec.modelId} (${rec.provider}, ${rec.tier})\n`;
        output += `   Score: ${rec.score.toFixed(1)} (raw: ${rec.rawScore}) | Est. Cost: $${rec.outputPrice}/M output tokens\n\n`;

        if (fb) {
          output += `🔄 Fallback: ${fb.modelId} (${fb.provider}, ${fb.tier})\n`;
          output += `   Score: ${fb.score.toFixed(1)} (raw: ${fb.rawScore}) | Est. Cost: $${fb.outputPrice}/M output tokens\n\n`;
        }

        output += `Top 5 Candidates:\n`;
        for (const s of result.allScores) {
          output += `  ${s.modelId.padEnd(22)} ${s.tier.padEnd(10)} Score: ${s.score.toFixed(1).padStart(6)}  Raw: ${String(s.rawScore).padStart(3)}  $${s.outputPrice}/M\n`;
        }

        return output;
      },
    },

    {
      name: "log_routing_decision",
      description: "Log a model routing decision for progressive learning. Returns formatted insight params to persist via document_insight.",
      parameters: {
        type: "object",
        properties: {
          task: { type: "string", description: "Task description" },
          category: { type: "string", description: "Task category", enum: Object.keys(WEIGHT_PROFILES) },
          complexity: { type: "string", description: "S/M/L/XL", enum: ["S", "M", "L", "XL"] },
          model_selected: { type: "string", description: "Model ID that was selected" },
          score: { type: "number", description: "Model's final score" },
          fallback_model: { type: "string", description: "Fallback model ID" },
          budget_mode: { type: "string", description: "Budget mode used", enum: ["cost-optimized", "balanced", "quality-first"] },
          constraints: { type: "string", description: "Active constraints (e.g., 'domain-sensitive, writes-code')" },
          outcome: { type: "string", description: "Post-execution outcome", enum: ["pending", "success", "partial", "failure", "retry-needed"] },
        },
        required: ["task", "category", "model_selected"],
      },
      handler: async (args) => {
        const entry = [
          `### Model Routing Decision`,
          ``,
          `- **Task:** ${args.task}`,
          `- **Category:** ${args.category} | **Complexity:** ${args.complexity || "M"}`,
          `- **Budget:** ${args.budget_mode || "balanced"}`,
          `- **Selected:** ${args.model_selected} (score: ${args.score || "N/A"})`,
          `- **Fallback:** ${args.fallback_model || "none"}`,
          `- **Constraints:** ${args.constraints || "none"}`,
          `- **Outcome:** ${args.outcome || "pending"}`,
          ``,
        ].join("\n");

        return `Routing decision formatted. Call document_insight to persist:\n` +
          `  category: "decision"\n` +
          `  title: "Model routing: ${args.category} → ${args.model_selected}"\n` +
          `  tags: ["model-routing", "${args.category}", "${args.model_selected}", "${args.budget_mode || "balanced"}"]\n` +
          `  content:\n${entry}`;
      },
    },

    {
      name: "mine_routing_history",
      description: "Generate SQL queries to mine model routing history from the Session Store for cross-session analysis.",
      parameters: {
        type: "object",
        properties: {
          category: { type: "string", description: "Filter by task category (optional)", enum: [...Object.keys(WEIGHT_PROFILES), "all"] },
          model: { type: "string", description: "Filter by model ID (optional)" },
          days: { type: "number", description: "Look back N days (default: 30)" },
        },
        required: [],
      },
      handler: async (args) => {
        const category = args.category && args.category !== "all" ? args.category : null;
        const model = args.model || null;
        const days = args.days || 30;

        let matchTerms = ["model-routing"];
        if (category) matchTerms.push(category);
        if (model) matchTerms.push(model);

        let output = `📊 Model Routing History Mining\n`;
        output += `Filters: category=${category || "all"}, model=${model || "all"}, days=${days}\n\n`;
        output += `Run these queries against the session_store database:\n\n`;

        output += `### Routing decisions (FTS search)\n\`\`\`sql\n`;
        output += `SELECT content, session_id, source_type FROM search_index WHERE search_index MATCH '${matchTerms.join(" AND ")}' ORDER BY rank LIMIT 30;\n\`\`\`\n\n`;

        output += `### Routing outcomes distribution\n\`\`\`sql\n`;
        output += `SELECT content, session_id FROM search_index WHERE search_index MATCH 'model-routing AND (excellent OR good OR adequate OR poor OR success OR failure)' ORDER BY rank LIMIT 30;\n\`\`\`\n\n`;

        output += `### Sessions with model overrides\n\`\`\`sql\n`;
        output += `SELECT s.id, s.summary, substr(t.assistant_response, 1, 300) as context FROM sessions s JOIN turns t ON t.session_id = s.id WHERE (t.assistant_response LIKE '%model%override%' OR t.assistant_response LIKE '%suggest_model%') AND t.timestamp >= date('now', '-${days} days') ORDER BY t.timestamp DESC LIMIT 20;\n\`\`\`\n\n`;

        output += `### High-turn sessions (possible wrong model choice)\n\`\`\`sql\n`;
        output += `SELECT s.id, s.summary, COUNT(t.turn_index) as turns FROM sessions s JOIN turns t ON t.session_id = s.id WHERE t.timestamp >= date('now', '-${days} days') GROUP BY s.id, s.summary HAVING turns > 15 ORDER BY turns DESC LIMIT 20;\n\`\`\`\n\n`;

        output += `**Instructions:** Execute each query using the sql tool with database: "session_store". Aggregate results to identify patterns.`;

        return output;
      },
    },
  ],
});
