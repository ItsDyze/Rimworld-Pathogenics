# MVP: Hermes-Managed Loop

This document validates the first end-to-end Hermes-managed workflow.

## Architecture

- **Hermes**: The manager agent that orchestrates the workflow
- **OpenHands**: The worker agent that executes tasks
- **GitHub App**: Provides credentials for branch and PR operations
- **Plane**: Source of truth for task tracking
- **Human Review**: Required before merge

## Workflow

1. Hermes receives a task from Plane
2. Hermes delegates to OpenHands for execution
3. OpenHands uses GitHub App credentials to create branches and PRs
4. Human review is required before any merge
5. Plane tracks the task status throughout the workflow