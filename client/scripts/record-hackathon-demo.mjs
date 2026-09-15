import { chromium } from "playwright";
import { mkdir, rm } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

const directory = path.dirname(fileURLToPath(import.meta.url));
const videoDirectory = path.resolve(directory, "..", "..", "video");
const temporaryDirectory = path.join(videoDirectory, ".hackathon-recording");
const output = path.join(videoDirectory, "Context-IQ-Hackathon-Demo.webm");
const edge = "C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe";

await rm(temporaryDirectory, { recursive: true, force: true });
await mkdir(temporaryDirectory, { recursive: true });

const browser = await chromium.launch({ executablePath: edge, headless: true });
const context = await browser.newContext({
  viewport: { width: 1440, height: 900 },
  recordVideo: {
    dir: temporaryDirectory,
    size: { width: 1440, height: 900 },
  },
});
const page = await context.newPage();
const video = page.video();
const pause = (milliseconds) => page.waitForTimeout(milliseconds);

const showSlide = async ({ eyebrow, title, subtitle, body, accent = "#56c7ff" }) => {
  await page.setContent(`
    <!doctype html>
    <html lang="en">
      <head>
        <meta charset="utf-8">
        <style>
          * { box-sizing: border-box; }
          body {
            margin: 0;
            min-height: 100vh;
            color: #f7fbff;
            font-family: "Segoe UI", Arial, sans-serif;
            background:
              radial-gradient(circle at 80% 15%, ${accent}38, transparent 32%),
              linear-gradient(135deg, #071426 0%, #102b49 55%, #0d3b56 100%);
          }
          main {
            min-height: 100vh;
            display: flex;
            flex-direction: column;
            justify-content: center;
            padding: 76px 105px;
          }
          .eyebrow {
            color: ${accent};
            font-size: 22px;
            font-weight: 700;
            letter-spacing: .16em;
            text-transform: uppercase;
          }
          h1 {
            max-width: 1120px;
            margin: 18px 0 16px;
            font-size: 68px;
            line-height: 1.04;
          }
          .subtitle {
            max-width: 1040px;
            color: #c8daeb;
            font-size: 30px;
            line-height: 1.35;
          }
          .body {
            margin-top: 38px;
            max-width: 1180px;
          }
          .command {
            padding: 26px 30px;
            border: 1px solid #ffffff29;
            border-radius: 15px;
            color: #d8f4ff;
            background: #03101ecc;
            font: 23px/1.55 Consolas, monospace;
            box-shadow: 0 18px 55px #00000042;
            white-space: pre-wrap;
          }
          .flow {
            display: grid;
            grid-template-columns: repeat(5, 1fr);
            gap: 14px;
            align-items: center;
          }
          .node {
            min-height: 122px;
            display: grid;
            place-items: center;
            padding: 18px;
            border: 1px solid #ffffff2b;
            border-radius: 14px;
            background: #ffffff10;
            text-align: center;
            font-size: 22px;
            font-weight: 650;
          }
          .arrow { color: ${accent}; text-align: center; font-size: 34px; }
          .pill-row { display: flex; gap: 16px; flex-wrap: wrap; }
          .pill {
            padding: 14px 20px;
            border: 1px solid #ffffff2e;
            border-radius: 999px;
            color: #d9eafb;
            background: #ffffff12;
            font-size: 21px;
          }
          footer {
            position: fixed;
            right: 40px;
            bottom: 30px;
            color: #93adc4;
            font-size: 18px;
          }
        </style>
      </head>
      <body>
        <main>
          <div class="eyebrow">${eyebrow}</div>
          <h1>${title}</h1>
          <div class="subtitle">${subtitle}</div>
          <div class="body">${body}</div>
        </main>
        <footer>Context IQ · Hackathon demo</footer>
      </body>
    </html>
  `);
};

await showSlide({
  eyebrow: "The challenge",
  title: "Enterprise context is scattered across screens, APIs, and repositories.",
  subtitle: "Context IQ turns trusted product capabilities into safe, conversational workflows.",
  body: `<div class="pill-row">
    <div class="pill">Grounded answers</div>
    <div class="pill">Typed tools</div>
    <div class="pill">Validated navigation</div>
    <div class="pill">Read-only by default</div>
  </div>`,
});
await pause(7000);

await showSlide({
  eyebrow: "Install in one command",
  title: "Point Context IQ at an existing React and .NET repository.",
  subtitle: "The installer scans source without executing product code and writes only to a separate output directory.",
  body: `<div class="command">.\\scripts\\install-context-iq.ps1 \`
  -TargetRepository C:\\path\\to\\product \`
  -Branch main \`
  -ProductName "Service Workspace" \`
  -AssistantName "Workspace Assistant"</div>`,
});
await pause(9000);

await page.goto("http://127.0.0.1:5173", { waitUntil: "networkidle" });
await pause(2500);
await page.getByRole("button", { name: "Open Context IQ" }).click();
await pause(1600);
await page.getByRole("button", { name: "Show my recent requests" }).click();
await pause(700);
await page.getByRole("button", { name: "Send" }).click();
await page.getByText("Found 3 recent requests created by alex.").waitFor();
await pause(3500);

const selectedCard = page.locator(".result-card").filter({ hasText: "REQ-1042" });
await selectedCard.evaluate((element) => element.scrollIntoView({ block: "center" }));
await pause(1200);
await selectedCard.getByRole("button", { name: "Show in requests" }).click();
await pause(3000);

const composer = page.getByLabel("Ask Context IQ");
await composer.fill("List the review ticket for this request");
await pause(700);
await page.getByRole("button", { name: "Send" }).click();
await page.getByText("The review ticket for request REQ-1042 is REV-8421.").waitFor();
await pause(3000);
const queueButton = page.getByRole("button", { name: "Open review queue" });
await queueButton.evaluate((element) => element.scrollIntoView({ block: "center" }));
await pause(800);
await queueButton.click();
await pause(4000);

await page.goto("http://127.0.0.1:4319", { waitUntil: "networkidle" });
await pause(3000);
await page.locator("section").first().evaluate((element) => element.scrollIntoView());
await pause(3500);
await page.locator("section").nth(1).evaluate((element) => element.scrollIntoView());
await pause(3500);

await showSlide({
  eyebrow: "Architecture",
  title: "A reusable shell with product-owned trust boundaries.",
  subtitle: "Context IQ orchestrates the conversation; the product remains responsible for identity, authorization, business rules, and authoritative data.",
  body: `<div class="flow">
    <div class="node">React product</div><div class="arrow">→</div>
    <div class="node">Context IQ<br>typed workflows</div><div class="arrow">→</div>
    <div class="node">Authorized .NET APIs</div>
  </div>
  <div class="flow" style="margin-top:14px">
    <div class="node">Grounded UI</div><div class="arrow">←</div>
    <div class="node">Source metadata</div><div class="arrow">←</div>
    <div class="node">Systems of record</div>
  </div>`,
  accent: "#7ee6a2",
});
await pause(10000);

await showSlide({
  eyebrow: "Hackathon outcome",
  title: "From repository to governed assistant—without rewriting the product.",
  subtitle: "Discover safe operations, generate integration scaffolding, derive regression scenarios, and give users one conversational path through their work.",
  body: `<div class="pill-row">
    <div class="pill">Repository discovery</div>
    <div class="pill">Live assistant</div>
    <div class="pill">Generated dashboard</div>
    <div class="pill">PR test scenarios</div>
  </div>`,
  accent: "#ffd66b",
});
await pause(7000);

await context.close();
await video.saveAs(output);
await browser.close();
await rm(temporaryDirectory, { recursive: true, force: true });

console.log(output);
