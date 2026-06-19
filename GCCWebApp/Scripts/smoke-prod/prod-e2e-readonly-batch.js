// Post-deploy E2E — READ-ONLY batch against gccwebapp.com. Executes a chunk of the
// framework's Test IDs (AU/MP/WF/SR/IN/RP/SE/RG). No writes — UAT data untouched.
// Uses context.request (shares the test-env basic-auth gate + the login cookie), so no
// page-origin issues. Prints PASS/FAIL/BLOCKED per Test ID for the UAT tracker.
const { chromium } = require('playwright');
const BASE = (process.env.BASE_URL || 'https://gccwebapp.com').replace(/\/$/, '');
const GATE = { username: process.env.SMOKE_REMOTE_USER || 'gcc', password: process.env.SMOKE_REMOTE_PASS || 'Cert-Test-Access-2026!' };
const PWD = 'TestPass123!';
const R = [];
const rec = (id, status, notes) => { R.push({ id, status, notes }); const t = { PASS:'✅', FAIL:'❌', BLOCKED:'⛔', WARN:'⚠️ ', INFO:'ℹ️ ' }[status] || '·'; console.log(`${t} ${id} [${status}] ${notes}`); };

const newCtx = (browser) => browser.newContext({ httpCredentials: GATE, ignoreHTTPSErrors: true, viewport:{width:1440,height:900} });
async function login(ctx,u,p){ const page = await ctx.newPage(); await page.goto(BASE+'/Account/Login',{waitUntil:'domcontentloaded'}); await page.locator('#Email').fill(u); await page.locator('#Password').fill(p); await Promise.all([page.waitForLoadState('domcontentloaded'), page.locator('button[type="submit"]').first().click()]); await page.waitForTimeout(1000); const ok=!/\/Account\/Login/i.test(page.url()); return {ok, page}; }
async function apiJson(ctx,path){ const r=await ctx.request.get(BASE+path,{headers:{Accept:'application/json'}}); let j=null; try{j=await r.json();}catch{} return {s:r.status(), total:j?(j.totalCount??j.TotalCount??null):null, certs:(j&&(j.certificates||j.Certificates))||[]}; }

(async () => {
  const browser = await chromium.launch();
  try {
    // ---------- PUBLIC (anon) ----------
    const anon = await newCtx(browser);
    const pub = await apiJson(anon, '/api/certificate-tiles?page=1&pageSize=300');
    rec('MP-07/WF-02-pubload', pub.s===200 ? 'PASS':'FAIL', `public tile API HTTP ${pub.s}, certs=${pub.certs.length}, total=${pub.total}`);
    if (pub.s===200 && pub.certs.length){
      const partnerLeak = pub.certs.filter(c => { const v=(c.partner??c.Partner??''); const arr=c.partnerNames??c.PartnerNames??[]; return (v&&String(v).trim()) || (Array.isArray(arr)&&arr.length); });
      rec('MP-07-pubpartner', partnerLeak.length===0?'PASS':'FAIL', `public certs exposing a partner field: ${partnerLeak.length} (expect 0)`);
      const statuses = [...new Set(pub.certs.map(c=>c.status??c.Status).filter(Boolean))];
      const bad = statuses.filter(s=>!/active|client approved|republished/i.test(s));
      rec('WF-02-pubstatus', bad.length===0?'PASS':'FAIL', `public statuses=[${statuses.join(',')}] non-public=[${bad.join(',')||'none'}]`);
    }
    const extAnon = await anon.request.get(BASE+'/api/CertificateTileExternal?page=1&pageSize=5');
    rec('IN-08/SE-09', [401,302,403].includes(extAnon.status())?'PASS':'FAIL', `external API anon HTTP ${extAnon.status()} (expect 401/302/403)`);
    const head = await anon.request.get(BASE+'/');
    const h = head.headers();
    const secHdrs = ['x-frame-options','content-security-policy','x-content-type-options','strict-transport-security'].filter(x=>h[x]);
    rec('RG-07/SE-12-headers', secHdrs.length>=2?'PASS':'WARN', `present: [${secHdrs.join(', ')}]`);
    await anon.close();

    // ---------- ADMIN ----------
    let dbTotal = null;
    const ac = await newCtx(browser);
    const admin = await login(ac,'admin',PWD);
    rec('DS-02-admin', admin.ok?'PASS':'FAIL', 'admin login');
    if (admin.ok){
      const intAll = await apiJson(ac, '/api/certificate-tiles-internal?page=1&pageSize=300'); dbTotal = intAll.total;
      rec('IN-07-pagesize', (intAll.s===200 && intAll.certs.length>20)?'PASS':(intAll.s===200?'WARN':'FAIL'), `internal pageSize=300 -> ${intAll.certs.length} rows (not capped 20), total=${intAll.total}`);
      const byNum = await apiJson(ac, '/api/certificate-tiles-internal?page=1&pageSize=10&search='+encodeURIComponent('26-0173'));
      rec('SR-04-certnum', (byNum.s===200 && byNum.certs.length>=1)?'PASS':'FAIL', `search "26-0173" -> ${byNum.certs.length} result(s)`);
      const sym = await apiJson(ac, '/api/certificate-tiles-internal?page=1&pageSize=10&search='+encodeURIComponent('3M™'));
      rec('SR-01-symbol', (sym.s===200 && (sym.total>0||sym.certs.length>0))?'PASS':'FAIL', `search "3M™" -> total=${sym.total}`);
    }
    await ac.close();

    // ---------- ANALYST (RBAC) ----------
    const anc = await newCtx(browser);
    const analyst = await login(anc,'analyst',PWD);
    rec('DS-02-analyst', analyst.ok?'PASS':'FAIL', 'analyst login');
    if (analyst.ok){
      const r = await anc.request.get(BASE+'/Admin/UserManagement', {maxRedirects:0}).catch(e=>({status:()=>'ERR',url:()=>String(e.message)}));
      const code = r.status();
      const denied = [401,403,302].includes(code);
      rec('AU-03-analyst-admin', denied?'PASS':'FAIL', `analyst -> /Admin/UserManagement HTTP ${code} (expect 401/403/302 redirect)`);
    }
    await anc.close();

    // ---------- EXTERNAL (company scope) ----------
    const exc = await newCtx(browser);
    const ext = await login(exc,'external',PWD);
    rec('DS-02-external', ext.ok?'PASS':'FAIL', 'external login');
    if (ext.ok){
      const e = await apiJson(exc, '/api/CertificateTileExternal?page=1&pageSize=300');
      const scoped = e.s===200 && e.total!=null && (dbTotal==null || e.total < dbTotal);
      rec('AU-05-external-scope', scoped?'PASS':'WARN', `external sees ${e.total} certs (DB total ${dbTotal}); scoped=${scoped}`);
      await ext.page.goto(BASE+'/Product/Management',{waitUntil:'domcontentloaded'}); await ext.page.waitForTimeout(800);
      const actionsVisible = await ext.page.evaluate(()=>{ const th=[...document.querySelectorAll('th')].find(t=>/action/i.test(t.textContent||'')); if(!th) return false; const s=getComputedStyle(th); return s.display!=='none'&&s.visibility!=='hidden'; });
      rec('AU-04-external-actions', actionsVisible?'FAIL':'PASS', `Actions column visible to external: ${actionsVisible} (expect false)`);
    }
    await exc.close();

    // ---------- PARTNERS (CR-171 isolation) ----------
    const partners = [
      { id:'MP-03', email:'partner@amazon.com', forbid:['Novi','Wayfair'] },
      { id:'MP-04', email:'perry.novi@example.com', forbid:['Amazon','Wayfair'] },
      { id:'MP-05', email:'pru.multi@example.com', forbid:['Wayfair'] },
    ];
    for (const pt of partners){
      const pc = await newCtx(browser);
      const pin = await login(pc, pt.email, PWD);
      if (!pin.ok){ rec(pt.id, 'BLOCKED', `${pt.email}/TestPass123! login failed on prod — supply prod creds`); await pc.close(); continue; }
      const r = await apiJson(pc, '/api/CertificateTileExternal?page=1&pageSize=300');
      const blob = JSON.stringify(r.certs);
      const leaks = pt.forbid.filter(n => new RegExp('\\b'+n+'\\b').test(blob));
      const access = r.total>0 && (dbTotal==null || r.total<dbTotal);
      rec(pt.id+'-access', access?'PASS':'FAIL', `${pt.email} sees ${r.total} certs (scoped)`);
      rec(pt.id+'-isolation', leaks.length===0?'PASS':'FAIL', leaks.length?`co-partner leak: ${leaks.join(',')}`:`no ${pt.forbid.join('/')} leak`);
      if (pt.email==='partner@amazon.com'){
        const csvr = await pc.request.get(BASE+'/Certificate/ExportCertificates?format=csv', {headers:{Accept:'text/csv'}});
        let distinct=null;
        if (csvr.status()===200){ const t=await csvr.text(); const lines=t.split(/\r?\n/).filter(l=>l.trim()); const hdr=(lines[0]||'').split(',').map(s=>s.replace(/"/g,'').trim().toLowerCase()); let ci=hdr.findIndex(x=>x.includes('cert')&&(x.includes('#')||x.includes('num'))); if(ci<0)ci=hdr.findIndex(x=>x.includes('number')); const set=new Set(); for(const l of lines.slice(1)){ const m=l.match(/("([^"]|"")*"|[^,]*)(,|$)/g)||[]; const v=(m[ci]||'').replace(/^"|",?$|,$/g,'').trim(); if(v) set.add(v);} distinct=set.size; }
        rec('RP-03-export-scope', (csvr.status()===200 && distinct!=null && (r.total==null||distinct<=r.total+1))?'PASS':'WARN', `Amazon export distinct certs=${distinct} vs tile scope=${r.total}`);
      }
      await pc.close();
    }
  } catch(e){ rec('ERR','FAIL', e.message); }
  finally {
    await browser.close();
    const c=s=>R.filter(r=>r.status===s).length;
    console.log(`\n=== ${c('PASS')} PASS / ${c('FAIL')} FAIL / ${c('BLOCKED')} BLOCKED / ${c('WARN')} WARN  (${R.length} checks) ===`);
    process.exit(c('FAIL')?1:0);
  }
})();
