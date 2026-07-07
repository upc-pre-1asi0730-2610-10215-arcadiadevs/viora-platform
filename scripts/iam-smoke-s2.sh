#!/usr/bin/env bash
# SDD audit/wa-os-backend-parity-closure-2026-07-02 WU1 Smoke Test
# Change: audit/wa-os-backend-parity-closure-2026-07-02 | WU1: role taxonomy alignment
# Tests: open sign-up (no admin gate), optional role field (default Grower,
#        explicit valid role, invalid role rejected), removed AssignRole route.
#
# Prerequisites:
#   - App must be running with the IamDataSeeder having run (roles Grower/
#     Specialist seeded).
#
# NOTE: this replaces the prior S2 script, which tested an admin-gated
# sign-up flow and the POST /api/v1/users/{id}/roles endpoint. Both were
# removed (REQ-1, spec obs #156): the Administrator role never had a
# bootstrap user assigned to it, making the old admin gate an unconditional
# sign-up deadlock. Sign-up is now unconditionally open in every
# environment, and role assignment happens via an optional `role` field on
# sign-up (defaulting to "Grower") instead of a separate admin-only endpoint.

set -euo pipefail

BASE_URL="${BASE_URL:-http://localhost:5000}"
PASS=0
FAIL=0

log_pass() { echo "  ✅ PASS: $1"; PASS=$((PASS + 1)); }
log_fail() { echo "  ❌ FAIL: $1"; FAIL=$((FAIL + 1)); }

curl_silent() {
    curl -s -o /dev/null -w "%{http_code}" "$@"
}

curl_body() {
    curl -s "$@"
}

echo "=== IAM Smoke Test (WU1 — Role Taxonomy Alignment) ==="
echo "Base URL: $BASE_URL"
echo ""

# ---------------------------------------------------------------
# 1. Sign-up is unconditionally open (no admin gate, any environment)
# ---------------------------------------------------------------
echo "--- 1. Sign-up is open (no Authorization header, no admin gate) ---"

DEFAULT_ROLE_USERNAME="default-role-$(date +%s)"
HTTP=$(curl_silent -X POST "$BASE_URL/api/v1/auth/sign-up" \
    -H "Content-Type: application/json" \
    -d "{\"username\":\"$DEFAULT_ROLE_USERNAME\",\"password\":\"P@ssw0rd!\"}")
if [ "$HTTP" = "201" ]; then
    log_pass "Sign-up with omitted role returns 201 (defaults to Grower)"
else
    log_fail "Sign-up with omitted role returns $HTTP (expected 201)"
fi

# ---------------------------------------------------------------
# 2. Sign-up with an explicit valid role
# ---------------------------------------------------------------
echo "--- 2. Sign-up with explicit valid role ---"

SPECIALIST_USERNAME="specialist-$(date +%s)"
HTTP=$(curl_silent -X POST "$BASE_URL/api/v1/auth/sign-up" \
    -H "Content-Type: application/json" \
    -d "{\"username\":\"$SPECIALIST_USERNAME\",\"password\":\"P@ssw0rd!\",\"role\":\"Specialist\"}")
if [ "$HTTP" = "201" ]; then
    log_pass "Sign-up with role=Specialist returns 201"
else
    log_fail "Sign-up with role=Specialist returns $HTTP (expected 201)"
fi

# ---------------------------------------------------------------
# 3. Sign-up with an invalid/retired role string is rejected
# ---------------------------------------------------------------
echo "--- 3. Sign-up with invalid role → 400 ---"

INVALID_ROLE_USERNAME="invalid-role-$(date +%s)"
HTTP=$(curl_silent -X POST "$BASE_URL/api/v1/auth/sign-up" \
    -H "Content-Type: application/json" \
    -d "{\"username\":\"$INVALID_ROLE_USERNAME\",\"password\":\"P@ssw0rd!\",\"role\":\"Administrator\"}")
if [ "$HTTP" = "400" ]; then
    log_pass "Sign-up with retired role 'Administrator' returns 400"
else
    log_fail "Sign-up with retired role 'Administrator' returns $HTTP (expected 400)"
fi

# ---------------------------------------------------------------
# 4. Sign in and use the resulting token
# ---------------------------------------------------------------
echo "--- 4. Sign in ---"

SIGNIN_BODY=$(curl_body -X POST "$BASE_URL/api/v1/auth/sign-in" \
    -H "Content-Type: application/json" \
    -d "{\"username\":\"$DEFAULT_ROLE_USERNAME\",\"password\":\"P@ssw0rd!\"}")
TOKEN=$(echo "$SIGNIN_BODY" | grep -o '"token":"[^"]*"' | cut -d'"' -f4)
if [ -n "$TOKEN" ]; then
    log_pass "Sign-in returned a token"
else
    log_fail "Sign-in failed: $SIGNIN_BODY"
fi

# ---------------------------------------------------------------
# 5. GET /api/v1/users still works (seed + auth sanity check)
# ---------------------------------------------------------------
echo "--- 5. GET all users (seed check) ---"

HTTP=$(curl_silent "$BASE_URL/api/v1/users" \
    -H "Authorization: Bearer ${TOKEN:-}")
if [ "$HTTP" = "200" ]; then
    log_pass "GET users returns 200 (seed data accessible)"
else
    log_fail "GET users returns $HTTP (expected 200)"
fi

# ---------------------------------------------------------------
# 6. The AssignRole endpoint no longer exists
# ---------------------------------------------------------------
echo "--- 6. POST /api/v1/users/{id}/roles is removed → 404 ---"

if [ -n "$TOKEN" ]; then
    HTTP=$(curl_silent -X POST "$BASE_URL/api/v1/users/1/roles" \
        -H "Authorization: Bearer $TOKEN" \
        -H "Content-Type: application/json" \
        -d '{"roleName":"Grower"}')
    if [ "$HTTP" = "404" ]; then
        log_pass "AssignRole route returns 404 (route removed, not 401/403)"
    else
        log_fail "AssignRole route returns $HTTP (expected 404 — route should no longer exist)"
    fi
else
    log_fail "No token available to test the removed AssignRole route"
fi

# ---------------------------------------------------------------
# Summary
# ---------------------------------------------------------------
echo ""
echo "=== Results ==="
echo "Passed: $PASS"
echo "Failed: $FAIL"

if [ "$FAIL" -gt 0 ]; then
    echo "SMOKE TEST FAILED"
    exit 1
else
    echo "SMOKE TEST PASSED"
    exit 0
fi
