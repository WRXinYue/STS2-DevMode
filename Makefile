# DevMode — build pipeline
#
#   build   → artifacts under repo build/DevMode/  (CI-safe, no game writes)
#   deploy  → copy those artifacts into the game mods dir
#   sync    → build + deploy (default local dev loop)

DOTNET ?= dotnet

# Read version from DevMode.json
VERSION := $(shell powershell -NoProfile -Command "(Get-Content DevMode.json | ConvertFrom-Json).version")

MOD_MAIN := DevMode.csproj

DEPLOY_TO_GAME := /p:DeployToGame=true

.PHONY: help init build deploy sync compile pck publish zip clean

help:
	@echo DevMode — targets
	@echo.
	@echo   init       detect STS2 + Godot, generate local.props + .vscode
	@echo   sync       build then deploy
	@echo   build      write build/DevMode/ only (no game)
	@echo   deploy     dotnet publish with DeployToGame=true
	@echo   compile    dotnet build to game mods (no .pck)
	@echo   pck        dotnet publish to game mods + .pck
	@echo   publish    build + create GitHub Release (requires gh CLI)
	@echo   zip        build + package into build/DevMode-vX.X.X.zip
	@echo   clean      remove build/ + dotnet clean

init:
	powershell -NoProfile -ExecutionPolicy Bypass -File scripts/init.ps1

build:
	$(DOTNET) publish $(MOD_MAIN)

deploy:
	$(DOTNET) publish $(DEPLOY_TO_GAME) $(MOD_MAIN)

sync: build deploy

compile:
	$(DOTNET) build $(DEPLOY_TO_GAME) $(MOD_MAIN)

pck:
	$(DOTNET) publish $(DEPLOY_TO_GAME) $(MOD_MAIN)

clean:
	@if exist build rmdir /s /q build
	$(DOTNET) clean DevMode.sln

publish:
	powershell -NoProfile -ExecutionPolicy Bypass -File scripts/publish-release.ps1 $(if $(VERSION),-Version $(VERSION),)

# ── zip: build + package into build/DevMode-vX.X.X.zip ──
ZIP_NAME := build\DevMode-v$(VERSION).zip
DIST_DIR := build\dist\DevMode

zip: build
	@if exist build\dist rmdir /s /q build\dist
	@mkdir "$(DIST_DIR)"
	@mkdir "$(DIST_DIR)\localization"
	@mkdir "$(DIST_DIR)\editor"
	@mkdir "$(DIST_DIR)\scripts"
	@copy /y build\DevMode\DevMode.dll "$(DIST_DIR)\" >nul
	@copy /y build\DevMode\DevMode.deps.json "$(DIST_DIR)\" >nul
	@copy /y DevMode.json "$(DIST_DIR)\" >nul
	@copy /y src\Localization\*.json "$(DIST_DIR)\localization\" >nul
	@xcopy /s /y /q editor\* "$(DIST_DIR)\editor\" >nul
	@if exist "$(ZIP_NAME)" del "$(ZIP_NAME)"
	python -c "import zipfile,os;z=zipfile.ZipFile('$(ZIP_NAME)','w',zipfile.ZIP_DEFLATED);[z.write(os.path.join(r,f),os.path.join(os.path.relpath(r,'build\\dist'),f)) for r,_,fs in os.walk('build\\dist\\DevMode') for f in fs];z.close()"
	@echo.
	@echo Done: $(ZIP_NAME)
	@echo Install: extract into "Slay the Spire 2\mods\"
