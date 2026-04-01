# DevMode — build pipeline
#
#   build   → artifacts under repo build/DevMode/  (CI-safe, no game writes)
#   deploy  → copy those artifacts into the game mods dir
#   sync    → build + deploy (default local dev loop)

DOTNET ?= dotnet

MOD_MAIN := DevMode.csproj

DEPLOY_TO_GAME := /p:DeployToGame=true

.PHONY: help init build deploy sync compile pck clean

help:
	@echo DevMode — targets
	@echo.
	@echo   init       detect STS2 + Godot, generate local.props + .vscode
	@echo   sync       build then deploy
	@echo   build      write build/DevMode/ only (no game)
	@echo   deploy     dotnet publish with DeployToGame=true
	@echo   compile    dotnet build to game mods (no .pck)
	@echo   pck        dotnet publish to game mods + .pck
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
