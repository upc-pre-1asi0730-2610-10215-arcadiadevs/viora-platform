#!/usr/bin/env bash
# SDD iam-auth-jwt Smear Smoke Test
# Change: iam-auth-jwt | Slice: S1 foundation
# Tests: sign-up, sign-in (happy + failure), token auth, expired token

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

echo "=== IAM Smoke Test (S1) ==="
echo "Base URL: $BASE_URL"
echo ""

# ---------------------------------------------------------------
# 1. Sign-up → 201
# ---------------------------------------------------------------
echo "--- 1. Sign-up happy path ---"
HTTP=$(curl_silent -X POST "$BASE_URL/api/v1/authentication/sign-up" \
    -H "Content-Type: application/json" \
    -d '{"username":"alice","password":"P@ssw0rd!"}')
if [ "$HTTP" = "201" ]; then
    log_pass "Sign-up returns 201"
else
    log_fail "Sign-up returns $HTTP (expected 201)"
fi

# Get the sign-up response body
SIGNUP_BODY=$(curl_body -X POST "$BASE_URL/api/v1/authentication/sign-up" \
    -H "Content-Type: application/json" \
    -d '{"username":"bob","password":"P@ssw0rd!"}')
echo "  Sign-up body: $SIGNUP_BODY"

# ---------------------------------------------------------------
# 2. Sign-up duplicate username → 400 Iam.UsernameAlreadyTaken
# ---------------------------------------------------------------
echo "--- 2. Sign-up duplicate username ---"
HTTP=$(curl_silent -X POST "$BASE_URL/api/v1/authentication/sign-up" \
    -H "Content-Type: application/json" \
    -d '{"username":"alice","password":"P@ssw0rd!"}')
if [ "$HTTP" = "400" ]; then
    log_pass "Duplicate sign-up returns 400"
else
    log_fail "Duplicate sign-up returns $HTTP (expected 400)"
fi

# ---------------------------------------------------------------
# 3. Sign-up weak password → 400 Iam.WeakPassword
# ---------------------------------------------------------------
echo "--- 3. Sign-up weak password ---"
HTTP=$(curl_silent -X POST "$BASE_URL/api/v1/authentication/sign-up" \
    -H "Content-Type: application/json" \
    -d '{"username":"charlie","password":"short"}')
if [ "$HTTP" = "400" ]; then
    log_pass "Weak password returns 400"
else
    log_fail "Weak password returns $HTTP (expected 400)"
fi

# ---------------------------------------------------------------
# 4. Sign-in happy path → 200 with token
# ---------------------------------------------------------------
echo "--- 4. Sign-in happy path ---"
SIGNIN_BODY=$(curl_body -X POST "$BASE_URL/api/v1/authentication/sign-in" \
    -H "Content-Type: application/json" \
    -d '{"username":"alice","password":"P@ssw0rd!"}')
SIGNIN_HTTP=$(curl_silent -X POST "$BASE_URL/api/v1/authentication/sign-in" \
    -H "Content-Type: application/json" \
    -d '{"username":"alice","password":"P@ssw0rd!"}')
if [ "$SIGNIN_HTTP" = "200" ]; then
    log_pass "Sign-in returns 200"
    # Extract token
    TOKEN=$(echo "$SIGNIN_BODY" | grep -o '"token":"[^"]*"' | cut -d'"' -f4)
    if [ -n "$TOKEN" ]; then
        log_pass "Token present in response"
    else
        log_fail "No token in response body: $SIGNIN_BODY"
    fi
else
    log_fail "Sign-in returns $SIGNIN_HTTP (expected 200)"
    TOKEN=""
fi

# ---------------------------------------------------------------
# 5. Sign-in wrong password → 401 Iam.InvalidCredentials
# ---------------------------------------------------------------
echo "--- 5. Sign-in wrong password ---"
HTTP=$(curl_silent -X POST "$BASE_URL/api/v1/authentication/sign-in" \
    -H "Content-Type: application/json" \
    -d '{"username":"alice","password":"WRONG"}')
if [ "$HTTP" = "401" ]; then
    log_pass "Wrong password returns 401"
else
    log_fail "Wrong password returns $HTTP (expected 401)"
fi

# ---------------------------------------------------------------
# 6. Sign-in unknown user → 401 Iam.InvalidCredentials
# ---------------------------------------------------------------
echo "--- 6. Sign-in unknown user ---"
HTTP=$(curl_silent -X POST "$BASE_URL/api/v1/authentication/sign-in" \
    -H "Content-Type: application/json" \
    -d '{"username":"nobody","password":"P@ssw0rd!"}')
if [ "$HTTP" = "401" ]; then
    log_pass "Unknown user returns 401"
else
    log_fail "Unknown user returns $HTTP (expected 401)"
fi

# ---------------------------------------------------------------
# 7. GET users/1 without token → 401 Iam.TokenRequired
# ---------------------------------------------------------------
echo "--- 7. GET users without token ---"
HTTP=$(curl_silent "$BASE_URL/api/v1/users/1")
if [ "$HTTP" = "401" ]; then
    log_pass "No token returns 401"
else
    log_fail "No token returns $HTTP (expected 401)"
fi

# ---------------------------------------------------------------
# 8. GET users/1 with valid token → 200
# ---------------------------------------------------------------
echo "--- 8. GET users with valid token ---"
if [ -n "$TOKEN" ]; then
    HTTP=$(curl_silent "$BASE_URL/api/v1/users/1" \
        -H "Authorization: Bearer $TOKEN")
    if [ "$HTTP" = "200" ] || [ "$HTTP" = "404" ]; then
        log_pass "Valid token returns $HTTP (authorized, user may or may not exist)"
    else
        log_fail "Valid token returns $HTTP (expected 200 or 404)"
    fi
else
    log_fail "Skipped: no token from sign-in"
fi

# ---------------------------------------------------------------
# 9. GET users with expired token → 401 Iam.TokenExpired
# ---------------------------------------------------------------
echo "--- 9. GET users with expired token ---"
EXPIRED_TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzaWQiOiIxIiwibmFtZSI6ImFsaWNlIiwiZXhwIjoxNTE2MjM5MDIyfQ.invalid"
HTTP=$(curl_silent "$BASE_URL/api/v1/users/1" \
    -H "Authorization: Bearer $EXPIRED_TOKEN")
if [ "$HTTP" = "401" ]; then
    log_pass "Expired token returns 401"
else
    log_fail "Expired token returns $HTTP (expected 401)"
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
