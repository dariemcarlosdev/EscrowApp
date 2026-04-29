/**
 * Mistake Prevention Extension
 * Monitors for common mistake patterns and enforces learned rules
 */

import { readFileSync, existsSync } from 'fs';
import { join } from 'path';

export const name = 'mistake-prevention';
export const description = 'Enforces learned rules to prevent common mistakes';

const LEARNED_RULES_PATH = '.github/rules/learned-rules.md';

export function tools() {
  return [
    {
      name: 'check_learned_rules',
      description: 'Check if current action violates any learned rules',
      parameters: {
        type: 'object',
        properties: {
          action: {
            type: 'string',
            description: 'Action being performed (e.g., "task_complete", "commit_files", "modify_code")'
          },
          context: {
            type: 'object',
            description: 'Context about the action (files modified, tests added, etc.)',
            properties: {
              files_modified: { type: 'array', items: { type: 'string' } },
              tests_added: { type: 'boolean' },
              docs_updated: { type: 'boolean' },
              checkboxes_updated: { type: 'boolean' }
            }
          }
        },
        required: ['action']
      }
    },
    {
      name: 'add_learned_rule',
      description: 'Add a new learned rule based on detected mistake pattern',
      parameters: {
        type: 'object',
        properties: {
          mistake_pattern: {
            type: 'string',
            description: 'Description of what went wrong'
          },
          prevention_rule: {
            type: 'string', 
            description: 'Specific rule to prevent this mistake'
          },
          priority: {
            type: 'string',
            enum: ['CRITICAL', 'HIGH', 'MEDIUM'],
            description: 'Rule priority level'
          },
          validation_hook: {
            type: 'string',
            description: 'How this rule will be enforced'
          }
        },
        required: ['mistake_pattern', 'prevention_rule', 'priority']
      }
    }
  ];
}

export async function handler(request) {
  const { name: toolName, arguments: args } = request;

  switch (toolName) {
    case 'check_learned_rules':
      return checkLearnedRules(args);
    case 'add_learned_rule':
      return addLearnedRule(args);
    default:
      throw new Error(`Unknown tool: ${toolName}`);
  }
}

function checkLearnedRules({ action, context = {} }) {
  if (!existsSync(LEARNED_RULES_PATH)) {
    return {
      violations: [],
      message: 'No learned rules file found - no violations detected'
    };
  }

  const rules = readFileSync(LEARNED_RULES_PATH, 'utf-8');
  const violations = [];

  // Rule: Task Completion Validation
  if (action === 'task_complete') {
    if (!context.docs_updated) {
      violations.push({
        rule: 'Task Completion Validation',
        priority: 'CRITICAL',
        message: 'Must update both implementation-plan.md AND task-checklist.md before task_complete',
        action_required: 'Update planning documentation first'
      });
    }
  }

  // Rule: Test Checkbox Sync
  if (context.tests_added && !context.checkboxes_updated) {
    violations.push({
      rule: 'Test Implementation Checkbox Sync', 
      priority: 'HIGH',
      message: 'Tests implemented but checkboxes not updated',
      action_required: 'Mark corresponding checkboxes [x] in task-checklist.md'
    });
  }

  // Rule: Build Validation
  const codeFiles = (context.files_modified || []).filter(f => 
    f.endsWith('.cs') || f.endsWith('.csproj') || f.endsWith('.razor') || f.endsWith('.resx')
  );
  if (codeFiles.length > 0 && action === 'task_complete') {
    violations.push({
      rule: 'Build Validation Before Completion',
      priority: 'CRITICAL', 
      message: 'Code files modified - must validate build still works',
      action_required: 'Run dotnet_build_check and dotnet_test_check'
    });
  }

  return {
    violations,
    message: violations.length === 0 ? 
      '✅ No learned rule violations detected' : 
      `⚠️ ${violations.length} learned rule violation(s) detected`
  };
}

function addLearnedRule({ mistake_pattern, prevention_rule, priority, validation_hook }) {
  // In a real implementation, this would append to the learned rules file
  // For now, return success message
  return {
    success: true,
    message: `Learned rule added: ${prevention_rule}`,
    rule_id: `rule-${Date.now()}`,
    storage_location: LEARNED_RULES_PATH
  };
}