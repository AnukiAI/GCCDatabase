# UAT Test Execution Checklist

**Quick Reference Guide for Manual Testing**  
**Application:** GCC Web Application  
**Test URL:** https://localhost:5101  

---

## 🔧 **Pre-Test Setup**
- [ ] Application running at test URL
- [ ] Test accounts available:
  - Admin: `admin@gcc.com` / `TestPass123!`
  - Analyst: `analyst@gcc.com` / `TestPass123!`
  - External: `external@company.com` / `TestPass123!`
- [ ] Browsers ready: Chrome, Firefox, Edge
- [ ] Mobile device or dev tools available

---

## 🔐 **Authentication Tests** (5 Tests)
- [ ] **TC-AUTH-001:** Valid user login works
- [ ] **TC-AUTH-002:** Invalid credentials rejected
- [ ] **TC-AUTH-003:** Empty fields show validation errors
- [ ] **TC-AUTH-004:** Logout works properly
- [ ] **TC-AUTH-005:** Session timeout handled

---

## 👤 **User Management Tests** (5 Tests)
- [ ] **TC-USER-001:** Admin can access user management
- [ ] **TC-USER-002:** Admin can create new users
- [ ] **TC-USER-003:** Admin can edit users
- [ ] **TC-USER-004:** Admin can delete users
- [ ] **TC-USER-005:** Non-admins cannot access user management

---

## 📜 **Certificate Management Tests** (6 Tests)
- [ ] **TC-CERT-001:** Certificate list displays correctly
- [ ] **TC-CERT-002:** Certificate details show properly
- [ ] **TC-CERT-003:** Authorized users can create certificates
- [ ] **TC-CERT-004:** Authorized users can edit certificates
- [ ] **TC-CERT-005:** Admins can delete certificates
- [ ] **TC-CERT-006:** External users cannot create/edit certificates

---

## 🔍 **Search & Filtering Tests** (4 Tests)
- [ ] **TC-SEARCH-001:** Basic search works
- [ ] **TC-SEARCH-002:** Company filtering works
- [ ] **TC-SEARCH-003:** Product type filtering works
- [ ] **TC-SEARCH-004:** No results handled gracefully

---

## 📱 **Mobile Responsiveness Tests** (3 Tests)
- [ ] **TC-MOBILE-001:** Mobile navigation works
- [ ] **TC-MOBILE-002:** Forms usable on mobile
- [ ] **TC-MOBILE-003:** Certificate views work on mobile

---

## ⚡ **Performance Tests** (2 Tests)
- [ ] **TC-PERF-001:** Pages load within 5 seconds
- [ ] **TC-PERF-002:** Large datasets handled smoothly

---

## 🔒 **Security Tests** (3 Tests)
- [ ] **TC-SEC-001:** SQL injection attempts blocked
- [ ] **TC-SEC-002:** XSS attempts prevented
- [ ] **TC-SEC-003:** Direct URL access controlled

---

## 📝 **Form Validation Tests** (3 Tests)
- [ ] **TC-VALID-001:** Email validation works
- [ ] **TC-VALID-002:** Required fields enforced
- [ ] **TC-VALID-003:** Date validation works

---

## 🌐 **Cross-Browser Tests** (3 Tests)
- [ ] **TC-BROWSER-001:** Works in Chrome
- [ ] **TC-BROWSER-002:** Works in Firefox
- [ ] **TC-BROWSER-003:** Works in Edge

---

## 📊 **Accessibility Tests** (2 Tests)
- [ ] **TC-ACCESS-001:** Keyboard navigation works
- [ ] **TC-ACCESS-002:** Screen reader compatible

---

## ❌ **Error Handling Tests** (2 Tests)
- [ ] **TC-ERROR-001:** 404 pages handled gracefully
- [ ] **TC-ERROR-002:** Form errors clear and helpful

---

## 📄 **File Operations Tests** (2 Tests)
- [ ] **TC-FILE-001:** File upload works (if available)
- [ ] **TC-FILE-002:** PDF downloads work

---

## 🎯 **Edge Cases Tests** (3 Tests)
- [ ] **TC-EDGE-001:** Long text input handled
- [ ] **TC-EDGE-002:** Rapid form submission prevented
- [ ] **TC-EDGE-003:** Browser back button works

---

## 📋 **Summary**

**Total Test Cases:** 42  

### **Test Results:**
- **Passed:** _____ / 42
- **Failed:** _____ / 42  
- **Partial:** _____ / 42

### **Critical Issues:**
1. _________________________________
2. _________________________________
3. _________________________________

### **Sign-off:**
**Tested By:** _________________________ **Date:** _________  
**UAT Approved:** [ ] YES [ ] NO  

---

### **Priority Legend:**
- 🔴 **Critical** - Must pass for production release
- 🟡 **High** - Should pass, minor issues acceptable
- 🟢 **Medium/Low** - Nice to have, can be addressed post-release

**For detailed step-by-step instructions, refer to the full MANUAL_UAT_TEST_CASES.md document.**