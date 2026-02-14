import { Config } from './constants/Config.js';
import { apiMgr } from './managers/ApiManager.js';
import { tokenMgr } from './managers/TokenManager.js';
import { sessionMgr } from './managers/SessionManager.js';
import { vaultMgr } from './managers/VaultManager.js';
import { dpopMgr } from './managers/DPoPManger.js';
import { stateHub } from './objects/EventHub.js';

let countdownInterval = null;

// Life Cycle
document.addEventListener('DOMContentLoaded', () => {
    console.log("OnCreate: Initializing App");
    init();
});

document.addEventListener('visibilitychange', () => {
    if (document.visibilityState === 'visible') {
        console.log("OnResume: App back in focus");
        // Logic: If token is likely expired, you could trigger a silent refresh here
    }
});

window.addEventListener('pagehide', () => {
    console.log("OnDestroy: Cleaning up resources");
    deinit();
});

async function init() {
    showLoader();
    
    await start();

    hideLoader();

    tokenMgr.isAuthenticated$.subscribe( ({isAuth, token}) => {
        if (isAuth) {
            startCountdown(token);
        } else {
            stopCountdown();
            clearOutput();
        }
    });
}

async function start() {
    
    await vaultMgr.init();
    
    const startIdx = sessionMgr.init();

    await tokenMgr.init(startIdx); 

    await dpopMgr.init(startIdx);

    apiMgr.init();

    console.log("🚀 System authorized and synchronized.");
}

function deinit() {
    
    close();

    if (countdownInterval) {
        clearInterval(countdownInterval);
        countdownInterval = null;
    }
}

export function close() {

    stateHub.dispose();
}

// Event Register
document.getElementById("loginBtn").addEventListener("click", ClickLogin);
document.getElementById("profileBtn").addEventListener("click", ClickProfile);
document.getElementById("postsBtn").addEventListener("click", ClickPosts);
document.getElementById("productsBtn").addEventListener("click", ClickProducts);
document.getElementById("logoutBtn").addEventListener("click", ClickLogout);

// Input Value
function getUsername() {
    return document.getElementById("username").value;
}
function getPassword() {
    return document.getElementById("password").value;
}

// Loader
function showLoader() {
  document.getElementById("loader").style.display = "block";
}
function hideLoader() {
  document.getElementById("loader").style.display = "none";
}

// Output Text
function clearOutput() {
    document.getElementById("output").textContent = "";
}
function showOutput(text) {
    document.getElementById("output").textContent = text;
}

// Countdown Text
function hideCountdown() {
    document.getElementById("countdown").textContent = "";
    document.getElementById("countdown").style.display = "none";
    document.getElementById("countdown").style.color = "transparent";
}
function showCountdown(text) {
    document.getElementById("countdown").style.display = "block";
    document.getElementById("countdown").textContent = text;
}
function setCountdownColor(color){
    document.getElementById("countdown").style.display = "block";
    document.getElementById("countdown").style.color = color;
}

//==========//

async function ClickLogin() {
    hideCountdown();
    clearOutput();
    showLoader();
    try {
        const res = await apiMgr.tokenApi.post("/login", {
            username: getUsername(),
            password: getPassword()
        });
        const { accessToken, refreshToken } = res.data.data;
        await tokenMgr.saveTokens( accessToken, refreshToken);
        console.log(`[Login] Logged in, tokens stored!`);
        showOutput(JSON.stringify(res.data, null, 2));
    } catch (err) {
        console.error("[Login] Failed", err);
        showOutput("Login failed: " + err);
    } finally {
        hideLoader();
    }
}

// Profile handler
async function ClickProfile() {
    clearOutput();
    const results = [];
    showLoader();
    console.log(`[Single] Request starting...`);
    try {
        const res = await apiMgr.authApi.get("/user");
        if (res && res.data) {
            console.log(`[Single] Request done!`);
            results.push(res.data);
        } else {
            console.log(`[Single] Request done!`, res);
            results.push("NO DATA!");
        }
    } catch (err) {
        console.error(`[Single] Request failed`, err);
        results.push(err);
    } finally {
        hideLoader();
    }
    showOutput(JSON.stringify(results, null, 2));
}

// Resource handler
async function ClickPosts() {
    clearOutput();
    const results = [];
    for (let i = 0; i < 10; i++) {    // to simulate 10 series fetch
        showLoader();
        console.log(`[Series] Request #${i + 1} starting...`);
        try {
            const res = await apiMgr.authApi.get(`/post/${i+1}`);
            console.log(`[Series] Request #${i + 1} done`);
            results.push(res.data);
        } catch (err) {
            console.error(`[Series] Request #${i + 1} failed`, err);
            results.push(err);
        } finally {
            hideLoader();
        }
    }
    showOutput(JSON.stringify(results, null, 2));
}

async function ClickProducts() {
    clearOutput();
    showLoader();
    
    const productPromises = Array.from({ length: 10 }).map((_, i) => {
        console.log(`[Parallel] Request #${i + 1} creating...`);
        return apiMgr.authApi.get(`/product/${i + 1}`);
    });
    
    // This waits for EVERY promise to either Resolve or Reject
    const results = await Promise.allSettled(productPromises);
    console.log(`[Parallel] Request all done`);

    // results is now an array of objects: 
    // { status: "fulfilled", value: ... } OR { status: "rejected", reason: ... }
    const formattedResults = results.map((result, i) => {
        if (result.status === "fulfilled") {
            console.log(`[Parallel] Request #${i + 1} formated`);
            return result.value.data; // The successful response
        } else {
            console.log(`[Parallel] Request #${i + 1} failed`, result.reason.message);
            return result.reason.message;
        }
    });

    showOutput(JSON.stringify(formattedResults, null, 2));
    hideLoader();
}

async function ClickLogout() {
    hideLoader();
    tokenMgr.clearTokens();
}

function stopCountdown() {
    if (countdownInterval) {
        clearInterval(countdownInterval);
        countdownInterval = null;
    }
    hideCountdown();
}

// Countdown helper
function startCountdown(token) {
    // stop any existing countdown first
    if (countdownInterval) {
        clearInterval(countdownInterval);
        countdownInterval = null;
    }

    let remaining = getRemainingSeconds(token);

    if (remaining < 0) {
        setCountdownColor("red");
        showCountdown("No Valid remaining time!");
        return;
    }

    if (remaining == 0) {
        setCountdownColor("red");
        showCountdown("Token expired!");
        return;
    }

    setCountdownColor("green");
    showCountdown(`Token expires in: ${remaining}s`);
    
    countdownInterval = setInterval(() => {
    
        remaining--;

        // Change color based on remaining time
        
        if (remaining > 30) {
            setCountdownColor("green");
        } else if (remaining > 10) {
            setCountdownColor("orange");
        } else {
            setCountdownColor("red");
        }

        if (remaining > 0) {
            showCountdown(`Token expires in: ${remaining}s`);
        } else {
            showCountdown("Token expired!");
            clearInterval(countdownInterval);
        }
        
    }, 1000);
}

function getRemainingSeconds(token) {
    if (!token) return -1;
    try {
        // JWT format: header.payload.signature
        const base64Url = token.split('.')[1]; 
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        const payload = JSON.parse(window.atob(base64));
        
        const now = Math.floor(Date.now() / 1000); // Current time in seconds
        return payload.exp - now; // Time remaining
    } catch (e) {
        return -1;
    }
}