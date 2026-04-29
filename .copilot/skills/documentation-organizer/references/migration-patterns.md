# Migration Patterns

> **Purpose:** Safe patterns for moving documentation files and updating references without breaking navigation.

## Migration Workflow

### Phase 1: Preservation Setup
```bash
# Create backup of current structure (safety net)
cp -r docs/ docs-backup-$(date +%Y%m%d)

# Create migration log for tracking changes
echo "# Documentation Migration Log" > docs-migration.md
echo "Date: $(date)" >> docs-migration.md
echo "" >> docs-migration.md
```

### Phase 2: Structure Creation
```bash
# Create module hierarchy FIRST (before moving files)
mkdir -p docs/modules/{authentication,payments,ui,system}
mkdir -p docs/platform/{architecture,operations,business}

# Validate structure creation
find docs/ -type d -name "modules" | grep -q modules && echo "✅ Modules created"
```

### Phase 3: File Migration with Tracking
```bash
# Move files with logging (example: authentication module)
echo "## Authentication Module Migration" >> docs-migration.md

# Individual feature moves
mv docs/features/user-login docs/modules/authentication/ && \
  echo "- user-login: features/ → modules/authentication/" >> docs-migration.md

mv docs/features/user-registration docs/modules/authentication/ && \
  echo "- user-registration: features/ → modules/authentication/" >> docs-migration.md

# Cross-cutting moves  
mv docs/cross-cutting/authentication docs/modules/authentication/identity-setup && \
  echo "- authentication setup: cross-cutting/ → modules/authentication/identity-setup" >> docs-migration.md
```

### Phase 4: Reference Updates
```bash
# Find all markdown files that might contain broken links
find docs/ -name "*.md" -exec grep -l "features/" {} \; > files-to-update.txt
find docs/ -name "*.md" -exec grep -l "cross-cutting/" {} \; >> files-to-update.txt

# Update references (requires manual review for context)
# Pattern: docs/features/user-login → docs/modules/authentication/user-login
# Pattern: docs/cross-cutting/auth → docs/modules/authentication/identity-setup
```

## Safe Migration Patterns

### Pattern 1: Atomic Module Migration
```bash
# Migrate one complete module at a time (not individual files)
migrate_authentication_module() {
    # Create module structure
    mkdir -p docs/modules/authentication
    
    # Move all related files together
    mv docs/features/user-login docs/modules/authentication/
    mv docs/features/user-registration docs/modules/authentication/  
    mv docs/cross-cutting/authentication docs/modules/authentication/identity-setup
    mv docs/cross-cutting/hybrid-identity docs/modules/authentication/
    
    # Create module README immediately
    create_module_readme "authentication"
    
    echo "✅ Authentication module migration complete"
}
```

### Pattern 2: Link Preservation Strategy
```bash
# Option A: Symlinks for backward compatibility (temporary)
ln -s modules/authentication/user-login docs/features/user-login-redirect
echo "Redirects to new location" > docs/features/user-login-redirect/MOVED.md

# Option B: Redirect documentation
create_redirect_doc() {
    local old_path=$1
    local new_path=$2
    
    cat > "$old_path/MOVED.md" << EOF
# Documentation Moved

This documentation has been relocated to improve organization:

**New Location:** [\`$new_path\`]($new_path)

**Reason:** Module-based organization for faster context discovery.

EOF
}
```

### Pattern 3: Batch Reference Updates
```python
# Python script for safe link updates
import re
import glob

def update_links_in_file(filepath):
    with open(filepath, 'r') as f:
        content = f.read()
    
    # Update patterns (customize for your structure)
    patterns = {
        r'docs/features/user-login': 'docs/modules/authentication/user-login',
        r'docs/features/user-registration': 'docs/modules/authentication/user-registration',
        r'docs/cross-cutting/authentication': 'docs/modules/authentication/identity-setup',
        r'docs/cross-cutting/localization': 'docs/modules/system/localization'
    }
    
    updated = content
    for old_pattern, new_path in patterns.items():
        updated = re.sub(old_pattern, new_path, updated)
    
    if updated != content:
        with open(filepath, 'w') as f:
            f.write(updated)
        print(f"✅ Updated links in {filepath}")

# Apply to all markdown files
for md_file in glob.glob('docs/**/*.md', recursive=True):
    update_links_in_file(md_file)
```

## Validation Checklist

### ✅ Pre-Migration Validation
- [ ] Backup created with timestamp
- [ ] Migration log initialized  
- [ ] New structure created and validated
- [ ] File inventory completed (`find docs/ -name "*.md" > pre-migration-files.txt`)

### ✅ During Migration
- [ ] Files moved atomically (complete modules, not individual files)
- [ ] Every move logged in migration document
- [ ] No files lost (validate counts: `wc -l pre-migration-files.txt vs post-migration-files.txt`)
- [ ] Module README created immediately after each module migration

### ✅ Post-Migration Validation  
- [ ] All original files exist in new locations
- [ ] Internal links updated and tested
- [ ] Master README navigation created
- [ ] Migration log documents all changes
- [ ] Backup can be restored if needed

## Common Migration Pitfalls

### ❌ **Moving Files Without Updating Links**
**Problem:** Broken internal navigation
**Solution:** Update references immediately after each module migration

### ❌ **Losing File History**
**Problem:** Git history breaks when using `rm` + `create` instead of `mv`
**Solution:** Use `git mv` for version control awareness

### ❌ **Incomplete Module Migration**
**Problem:** Related docs scattered across old and new locations  
**Solution:** Migrate complete modules atomically, not individual files

### ❌ **Missing Navigation Aids**
**Problem:** New structure is less discoverable than original
**Solution:** Create README indexes immediately after migration

## Emergency Rollback

```bash
# If migration fails, restore from backup
restore_backup() {
    if [ -d "docs-backup-$(date +%Y%m%d)" ]; then
        rm -rf docs/
        mv "docs-backup-$(date +%Y%m%d)" docs/
        echo "🔄 Rollback complete - documentation restored"
    else
        echo "❌ No backup found - manual recovery required"
    fi
}
```

**Rule:** Always create timestamped backups before any structural changes. Migration should be reversible.