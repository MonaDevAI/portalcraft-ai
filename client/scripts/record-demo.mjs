import { chromium } from "playwright";
import { mkdir, rm } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

const directory = path.dirname(fileURLToPath(import.meta.url));
const videoDirectory = path.resolve(directory, "..", "..", "video");
const temporaryDirectory = path.join(videoDirectory, ".recording");
const output = path.join(videoDirectory, "PortalCraft-AI-Generic-Live-Demo.webm");
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
const ask = async (message) => {
  const composer = page.getByLabel("Ask PortalCraft AI");
  await composer.fill(message);
  await pause(700);
  await page.getByRole("button", { name: "Send" }).click();
};

await page.goto("http://127.0.0.1:5173", { waitUntil: "networkidle" });
await pause(2500);
await page.getByRole("button", { name: "Open PortalCraft AI" }).click();
await pause(1800);

await page.getByRole("button", { name: "Show my recent requests" }).click();
await pause(800);
await page.getByRole("button", { name: "Send" }).click();
await page.getByText("Found 3 recent requests created by alex.").waitFor();
await pause(3500);

const selectedCard = page.locator(".result-card").filter({ hasText: "REQ-1042" });
await selectedCard.evaluate(element => element.scrollIntoView({ block: "center" }));
await pause(1200);
await selectedCard.getByRole("button", { name: "Show in requests" }).evaluate(button => button.click());
await pause(3000);

await ask("List the review ticket for this request");
await page.getByText("The review ticket for request REQ-1042 is REV-8421.").waitFor();
await pause(3000);
const queueButton = page.getByRole("button", { name: "Open review queue" });
await queueButton.evaluate(element => element.scrollIntoView({ block: "center" }));
await pause(1000);
await queueButton.evaluate(button => button.click());
await pause(3500);

await ask("Show request REQ-1035");
await page.getByText(/REQ-1035 is Draft/).waitFor();
await pause(2500);
await ask("List the review ticket for this request");
await page.getByText("Request REQ-1035 does not have a review ticket yet.").waitFor();
await pause(3000);

await ask("Approve request REQ-1042");
await page.getByText(/read-only in this sample/).waitFor();
await pause(3500);

await context.close();
await video.saveAs(output);
await browser.close();
await rm(temporaryDirectory, { recursive: true, force: true });

console.log(output);
