#!/usr/bin/env bash
# SDD iam-auth-jwt S3 Smoke Test
# Change: iam-auth-jwt | Slice: S3 facade
# Tests: Ensures the facade and existing S1+S2 endpoints still work correctly.
#         The facade itself is a DI service — not testable via curl directly.
#         This smoke test verifies that adding the facade did not break anything.

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

echo "=== IAM Smoke Test (S3 — Facade + Regression) ==="
echo "Base URL: $BASE_URL"
echo ""

# ---------------------------------------------------------------
# 1. Sign-up still works
# ---------------------------------------------------------------
echo "--- 1. Sign-up ---"
S3_USER="s3-smoke-$(date +%s)"
HTTP=$(curl_silent -X POST "$BASE_URL/api/v1/auth/sign-up" \
    -H "Content-Type: application/json" \
    -d "{\"username\":\"$S3_USER\",\"password\":\"P@ssw0rd!\"}")
if [ "$HTTP" = "201" ]; then
    log_pass "Sign-up returns 201"
else
    log_fail "Sign-up returns $HTTP (expected 201)"
fi

# ---------------------------------------------------------------
# 2. Sign-in still works
# ---------------------------------------------------------------
echo "--- 2. Sign-in ---"
SIGNIN_BODY=$(curl_body -X POST "$BASE_URL/api/v1/auth/sign-in" \
    -H "Content-Type: application/json" \
    -d "{\"username\":\"$S3_USER\",\"password\":\"P@ssw0rd!\"}")
SIGNIN_HTTP=$(curl_silent -X POST "$BASE_URL/api/v1/auth/sign-in" \
    -H "Content-Type: application/json" \
    -d "{\"username\":\"$S3_USER\",\"password\":\"P@ssw0rd!\"}")
if [ "$SIGNIN_HTTP" = "200" ]; then
    log_pass "Sign-in returns 200"
    TOKEN=$(echo "$SIGNIN_BODY" | grep -o '"token":"[^"]*"' | cut -d'"' -f4)
    if [ -n "$TOKEN" ]; then
        log_pass "Token present in response"
    else
        log_fail "No token in response body"
        TOKEN=""
    fi
else
    log_fail "Sign-in returns $SIGNIN_HTTP (expected 200)"
    TOKEN=""
fi

# ---------------------------------------------------------------
# 3. GET /api/v1/users without token → 401 (middleware still active)
# ---------------------------------------------------------------
echo "--- 3. GET users without token → 401 ---"
HTTP=$(curl_silent "$BASE_URL/api/v1/users/1")
if [ "$HTTP" = "401" ]; then
    log_pass "No token returns 401"
else
    log_fail "No token returns $HTTP (expected 401)"
fi

# ---------------------------------------------------------------
# 4. GET /api/v1/users with valid token → 200
# ---------------------------------------------------------------
echo "--- 4. GET users with valid token → 200 ---"
if [ -n "${TOKEN:-}" ]; then
    HTTP=$(curl_silent "$BASE_URL/api/v1/users/1" \
        -H "Authorization: Bearer $TOKEN")
    if [ "$HTTP" = "200" ] || [ "$HTTP" = "404" ]; then
        log_pass "Valid token returns $HTTP (authorized)"
    else
        log_fail "Valid token returns $HTTP (expected 200 or 404)"
    fi
else
    log_fail "Skipped: no token from sign-in"
fi

# ---------------------------------------------------------------
# 5. GET /api/v1/users (all users) with valid token → 200
# ---------------------------------------------------------------
echo "--- 5. GET all users with valid token → 200 ---"
if [ -n "${TOKEN:-}" ]; then
    HTTP=$(curl_silent "$BASE_URL/api/v1/users" \
        -H "Authorization: Bearer $TOKEN")
    if [ "$HTTP" = "200" ]; then
        log_pass "GET all users returns 200"
    else
        log_fail "GET all users returns $HTTP (expected 200)"
    fi
else
    log_fail "Skipped: no token from sign-in"
fi

# ---------------------------------------------------------------
# 6. Sign-up duplicate → 400 (still rejects)
# ---------------------------------------------------------------
echo "--- 6. Sign-up duplicate → 400 ---"
HTTP=$(curl_silent -X POST "$BASE_URL/api/v1/auth/sign-up" \
    -H "Content-Type: application/json" \
    -d "{\"username\":\"$S3_USER\",\"password\":\"P@ssw0rd!\"}")
if [ "$HTTP" = "400" ]; then
    log_pass "Duplicate sign-up returns 400"
else
    log_fail "Duplicate sign-up returns $HTTP (expected 400)"
fi

# ---------------------------------------------------------------
# 7. Sign-in wrong password → 401
# ---------------------------------------------------------------
echo "--- 7. Sign-in wrong password → 401 ---"
HTTP=$(curl_silent -X POST "$BASE_URL/api/v1/auth/sign-in" \
    -H "Content-Type: application/json" \
    -d "{\"username\":\"$S3_USER\",\"password\":\"WRONG\"}")
if [ "$HTTP" = "401" ]; then
    log_pass "Wrong password returns 401"
else
    log_fail "Wrong password returns $HTTP (expected 401)"
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
