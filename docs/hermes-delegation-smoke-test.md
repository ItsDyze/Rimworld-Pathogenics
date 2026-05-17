# Hermes Delegation Smoke Test

## Overview

This document describes the delegation architecture between Hermes and OpenHands.

## Roles

- **Hermes**: Acts as the manager agent, coordinating task delegation and workflow orchestration.
- **OpenHands**: Acts as the worker agent, executing delegated tasks and returning results.

## Authentication

GitHub App credentials are used for branch and pull request operations. This enables automated PR creation and branch management while maintaining security through app-based authentication.

## Workflow

1. Hermes delegates a task to OpenHands
2. OpenHands executes the task and creates necessary branches/PRs
3. Human review is required before any merge can proceed
4. Once approved, the change can be merged

## Human Review Requirement

All changes must be reviewed by a human before merging. This ensures code quality and security compliance.