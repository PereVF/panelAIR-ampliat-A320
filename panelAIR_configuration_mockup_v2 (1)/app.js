const welcomeScreen = document.getElementById("welcomeScreen");
const configurationScreen = document.getElementById("configurationScreen");
const continueButton = document.getElementById("continueButton");
const gearButton = document.getElementById("gearButton");
const touchButton = document.getElementById("touchButton");
const touchStatus = document.getElementById("touchStatus");
const browseButton = document.getElementById("browseButton");
const sfsPath = document.getElementById("sfsPath");
const installButton = document.getElementById("installButton");
const installStatus = document.getElementById("installStatus");
const autoLaunch = document.getElementById("autoLaunch");
const autoLaunchState = document.getElementById("autoLaunchState");

function showConfiguration() {
  welcomeScreen.classList.add("hidden");
  configurationScreen.classList.remove("hidden");
}

continueButton.addEventListener("click", showConfiguration);
gearButton.addEventListener("click", showConfiguration);

touchButton.addEventListener("click", () => {
  touchStatus.textContent = "This monitor is configured as the touch screen.";
  touchButton.textContent = "Touch Screen Configured";
});

browseButton.addEventListener("click", () => {
  // Browser mock-up only. In the real desktop app, connect this button
  // to the native file picker.
  sfsPath.focus();
  sfsPath.select();
});

installButton.addEventListener("click", () => {
  installButton.disabled = true;
  installButton.textContent = "Installing…";
  installStatus.textContent = "";

  window.setTimeout(() => {
    installButton.textContent = "SFS Link Installed";
    installStatus.textContent = "The latest version of SFS Link has been installed.";
  }, 900);
});


autoLaunch.addEventListener("change", () => {
  autoLaunchState.textContent = autoLaunch.checked ? "On" : "Off";
});
