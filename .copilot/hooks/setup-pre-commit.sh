#!/bin/bash
# Unified Pre-Commit Hook Setup Script
# Works for: Copilot CLI, Claude Code, and manual Git Hook setup
# Platform: Windows (Git Bash), macOS, Linux
# Purpose: Initialize portable pre-commit validation infrastructure

set -e

echo "🛡️  NexSynapse Portable Pre-Commit Hook Setup"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

# ─── Detect Environment ───
detect_environment() {
    if [ -n "$COPILOT_WORKSPACE" ]; then
        echo "📍 Environment: Copilot CLI detected"
        ENVIRONMENT="copilot"
    elif [ -n "$CLAUDE_CODE_ENV" ] || [ -f  "./.claude/settings.json" ]; then
        echo "📍 Environment: Claude Code detected"
        ENVIRONMENT="claude"
    else
        echo "📍 Environment: Manual/CLI setup"
        ENVIRONMENT="manual"
    fi
}

# ─── Setup Git Pre-Commit Hook (Always) ───
setup_git_hook() {
    echo ""
    echo "Step 1: Setting up Git pre-commit hook..."
    
    # Ensure .git/hooks directory exists
    mkdir -p .git/hooks
    
    # Copy the shell script hook
    if [ -f  ".github/hooks/pre-commit" ]; then
        cp .github/hooks/pre-commit .git/hooks/pre-commit
        chmod +x .git/hooks/pre-commit
        echo "✅ Git pre-commit hook installed"
        echo "   Location: .git/hooks/pre-commit"
    else
        echo "⚠️  .github/hooks/pre-commit not found"
    fi
}

# ─── Setup Copilot CLI Hooks ───
setup_copilot_cli() {
    echo ""
    echo "Step 2a: Setting up Copilot CLI pre-commit validation..."
    
    # Copy Copilot hook config
    if [ -f  ".github/hooks/pre-commit.yaml" ]; then
        echo "✅ Copilot CLI hook config found: .github/hooks/pre-commit.yaml"
        echo "   Copilot will auto-trigger validation on git commit"
    fi
    
    # If Copilot CLI is available, register the hook
    if command -v copilot &> /dev/null; then
        echo "✅ Copilot CLI command found"
        echo "   To enable auto-validation, configure Copilot:"
        echo "   $ copilot config set pre-commit.enabled=true"
    else
        echo "ℹ️  Copilot CLI not found in PATH"
        echo "   Install from: https://github.com/github/copilot-cli"
    fi
}

# ─── Setup Claude Code Hooks ───
setup_claude_code() {
    echo ""
    echo "Step 2b: Setting up Claude Code pre-commit validation..."
    
    # Copy Claude hook config
    if [ -f  ".claude/hooks/pre-commit.yaml" ]; then
        echo "✅ Claude Code hook config found: .claude/hooks/pre-commit.yaml"
        echo "   Claude will offer validation on git commit operations"
    fi
    
    # Create Claude Code settings if not present
    if [ ! -f ".claude/settings.json" ]; then
        mkdir -p .claude
        cat > .claude/settings.json << 'EOF'
{
  "commands": {
    "pre-commit-validate": {
      "description": "Run pre-commit validation",
      "file": ".claude/hooks/pre-commit.yaml"
    }
  },
  "on_commit": {
    "validation_enabled": true,
    "security_checks": true,
    "block_on_critical": true
  }
}
EOF
        echo "✅ Claude Code settings created: .claude/settings.json"
    else
        echo "✅ Claude Code settings already configured"
    fi
    
    # Note about Claude Code integration
    echo "   To enable auto-validation in Claude Code:"
    echo "   • Use /pre-commit-validate command before committing"
    echo "   • Or let Claude auto-offer validation on commit attempt"
}

# ─── Verify Setup ───
verify_setup() {
    echo ""
    echo "Step 3: Verifying setup..."
    echo ""
    
    SETUP_OK=0
    
    if [ -f  ".git/hooks/pre-commit" ] && [ -x ".git/hooks/pre-commit" ]; then
        echo "✅ Git pre-commit hook installed and executable"
        SETUP_OK=$((SETUP_OK + 1))
    else
        echo "❌ Git pre-commit hook not found or not executable"
    fi
    
    if [ -f  ".github/hooks/pre-commit.yaml" ]; then
        echo "✅ Copilot CLI hook config present"
        SETUP_OK=$((SETUP_OK + 1))
    fi
    
    if [ -f  ".claude/hooks/pre-commit.yaml" ]; then
        echo "✅ Claude Code hook config present"
        SETUP_OK=$((SETUP_OK + 1))
    fi
    
    if [ -f  ".claude/settings.json" ]; then
        echo "✅ Claude Code settings configured"
        SETUP_OK=$((SETUP_OK + 1))
    fi
    
    echo ""
    if [ $SETUP_OK -eq 4 ]; then
        echo "✅ Setup complete! Pre-commit hooks are ready."
    else
        echo "⚠️  Setup partially complete. Some components missing."
    fi
}

# ─── Print Usage Instructions ───
print_usage() {
    echo ""
    echo "📚 Usage Instructions"
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo ""
    
    case "$ENVIRONMENT" in
        copilot)
            echo "🔧 Copilot CLI Environment:"
            echo "   Pre-commit validation is now active."
            echo "   When you run: git commit"
            echo "   → Pre-commit hook will run automatically"
            echo ""
            echo "   To skip validation (NOT RECOMMENDED):"
            echo "   $ git commit --no-verify"
            ;;
        claude)
            echo "🔧 Claude Code Environment:"
            echo "   Pre-commit validation is now available."
            echo "   In Claude, use command: /pre-commit-validate"
            echo "   Or let Claude offer validation on commit"
            echo ""
            echo "   When you commit:"
            echo "   → Claude will offer to run validation"
            echo "   → Git hook will also run as backup"
            ;;
        manual)
            echo "🔧 Manual/CLI Environment:"
            echo "   Pre-commit validation is active via Git hook."
            echo "   When you run: git commit"
            echo "   → Pre-commit hook will run automatically"
            echo ""
            echo "   To skip validation (NOT RECOMMENDED):"
            echo "   $ git commit --no-verify"
            echo ""
            echo "   To manually validate before committing:"
            echo "   $ bash .github/hooks/pre-commit"
            ;;
    esac
    
    echo ""
    echo "📖 Full Documentation:"
    echo "   Read: .github/PORTABLE-COMMIT-WORKFLOW.md"
    echo ""
}

# ─── Main Execution ───
main() {
    detect_environment
    setup_git_hook
    
    # Setup both, regardless of detected environment (for portability)
    setup_copilot_cli
    setup_claude_code
    
    verify_setup
    print_usage
    
    echo ""
    echo "✨ Setup finished! Happy committing! 🚀"
}

main
