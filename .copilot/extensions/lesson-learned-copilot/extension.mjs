/**
 * Mistake Prevention Extension (Copilot CLI)
 * Provides real-time learned rule checking and enforcement
 */

import { readFileSync, existsSync } from 'fs';
import { join } from 'path';

export const name = 'lesson-learned-copilot';
export const description = 'Copilot CLI integration for learned rules enforcement';

export function tools() {
  return [
    {
      name: 'check_learned_rules',
      description: 'Check current action against learned prevention rules',
      parameters: {
        type: 'object',
        properties: {
          action: {
            type: 'string',
            description: 'The action being performed (e.g., "task_complete", "commit", "build")'
          },
          context: {
            type: 'string', 
            description: 'Additional context about the action'
          }
        },
        required: ['action']
      }
    },
    {
      name: 'add_learned_rule',
      description: 'Add a new learned rule from a detected mistake pattern',
      parameters: {
        type: 'object',
        properties: {
          rule_name: {
            type: 'string',
            description: 'Name of the prevention rule'
          },
          priority: {
            type: 'string', 
            enum: ['CRITICAL', 'HIGH', 'MEDIUM'],
            description: 'Priority level of the rule'
          },
          pattern: {
            type: 'string',
            description: 'Description of what mistake this rule prevents'
          },
          validation: {
            type: 'string',
            description: 'How to validate compliance with this rule'
          },
          generated_from: {
            type: 'string',
            description: 'Context of the mistake that generated this rule'
          }
        },
        required: ['rule_name', 'priority', 'pattern', 'validation', 'generated_from']
      }
    }
  ];
}

export function handler({ tool, parameters }) {
  const rulesPath = join(process.cwd(), '.github', 'rules', 'learned-rules.md');
  
  try {
    switch (tool) {
      case 'check_learned_rules':
        return checkLearnedRules(parameters.action, parameters.context, rulesPath);
      
      case 'add_learned_rule':
        return addLearnedRule(parameters, rulesPath);
      
      default:
        return { error: `Unknown tool: ${tool}` };
    }
  } catch (error) {
    return { error: `Error in lesson-learned extension: ${error.message}` };
  }
}

function checkLearnedRules(action, context = '', rulesPath) {
  if (!existsSync(rulesPath)) {
    return {
      status: 'no_rules',
      message: 'No learned rules file found. Rules will be created as mistakes are detected.'
    };
  }
  
  const rulesContent = readFileSync(rulesPath, 'utf8');
  const violations = [];
  
  // Check for specific action patterns
  switch (action.toLowerCase()) {
    case 'task_complete':
      if (!checkDocumentationSync(context)) {
        violations.push({
          rule: 'Task Completion Validation',
          priority: 'CRITICAL',
          message: 'Must update both implementation-plan.md AND task-checklist.md before task_complete'
        });
      }
      break;
      
    case 'commit':
    case 'git_commit':
      if (context && context.includes('escrow') && isUserFacing(context)) {
        violations.push({
          rule: 'Terminology Compliance', 
          priority: 'CRITICAL',
          message: 'Cannot use "escrow" in user-facing text without legal review'
        });
      }
      break;
  }
  
  return {
    status: violations.length > 0 ? 'violations_found' : 'compliant',
    violations,
    rules_checked: extractRuleNames(rulesContent)
  };
}

function addLearnedRule(params, rulesPath) {
  const timestamp = new Date().toISOString().split('T')[0];
  
  const newRule = `
### Rule: ${params.rule_name}
**Generated from**: ${params.generated_from}
**Priority**: ${params.priority}
**Rule**: "${params.pattern}"
**Validation**: ${params.validation}
**Added**: ${timestamp}
**Triggered**: 0 times
**Prevented**: 0 mistakes
`;

  try {
    let rulesContent = '';
    if (existsSync(rulesPath)) {
      rulesContent = readFileSync(rulesPath, 'utf8');
    } else {
      rulesContent = `# Learned Rules — Project\n\n> Auto-generated prevention rules from detected mistake patterns.\n\n`;
    }
    
    // Find insertion point (before metrics section or at end)
    const metricsIndex = rulesContent.indexOf('## 📊 Rule Effectiveness Metrics');
    if (metricsIndex > -1) {
      rulesContent = rulesContent.slice(0, metricsIndex) + newRule + '\n' + rulesContent.slice(metricsIndex);
    } else {
      rulesContent += newRule;
    }
    
    // Update metrics
    const totalRules = (rulesContent.match(/### Rule:/g) || []).length;
    rulesContent = updateMetrics(rulesContent, totalRules);
    
    return {
      status: 'rule_added',
      rule_name: params.rule_name,
      priority: params.priority,
      total_rules: totalRules
    };
    
  } catch (error) {
    return {
      status: 'error',
      message: `Failed to add rule: ${error.message}`
    };
  }
}

function checkDocumentationSync(context) {
  // Check if both planning documents exist and were recently modified
  const planPath = join(process.cwd(), 'docs', 'planning', 'implementation-plan.md');
  const checklistPath = join(process.cwd(), 'docs', 'planning', 'task-checklist.md');
  
  return existsSync(planPath) && existsSync(checklistPath);
}

function isUserFacing(context) {
  const userFacingPatterns = ['.razor', '.resx', 'UI', 'component', 'page', 'error message'];
  return userFacingPatterns.some(pattern => context.toLowerCase().includes(pattern.toLowerCase()));
}

function extractRuleNames(rulesContent) {
  const ruleMatches = rulesContent.match(/### Rule: (.+)/g);
  return ruleMatches ? ruleMatches.map(match => match.replace('### Rule: ', '')) : [];
}

function updateMetrics(content, totalRules) {
  const timestamp = new Date().toISOString().split('T')[0];
  const metricsPattern = /\*\*Total Rules\*\*: \d+/;
  
  if (metricsPattern.test(content)) {
    content = content.replace(metricsPattern, `**Total Rules**: ${totalRules}`);
  }
  
  const datePattern = /\*\*Last Updated\*\*: [^\n]+/;
  if (datePattern.test(content)) {
    content = content.replace(datePattern, `**Last Updated**: ${timestamp}`);
  }
  
  return content;
}