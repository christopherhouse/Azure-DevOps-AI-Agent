# Chat History Reduction Configuration for Deployment

## Overview

The chat history reducer/summarization feature has been added to the backend to manage conversation context and token usage efficiently. This document outlines the environment variables that need to be set during deployment.

## Required Environment Variables

The following environment variables should be configured in the Container App environment:

### Chat History Reduction Settings

| Environment Variable | Description | Default Value | Required |
|---------------------|-------------|---------------|----------|
| `ChatHistoryReduction__Enabled` | Enable/disable chat history reduction | `true` | Yes |
| `ChatHistoryReduction__UseSummarization` | Use AI summarization (true) or simple truncation (false) | `true` | Yes |
| `ChatHistoryReduction__TargetCount` | Target number of messages after reduction | `15` | Yes |
| `ChatHistoryReduction__ThresholdCount` | Threshold to trigger reduction (message count) | `30` | Yes |
| `ChatHistoryReduction__KeepLastMessages` | Number of recent messages to always keep | `10` | Yes |
| `ChatHistoryReduction__EstimatedTokensPerMessage` | Estimated tokens per message for calculations | `200` | No |
| `ChatHistoryReduction__MaxContextTokens` | Maximum tokens allowed in context | `100000` | No |

## Configuration Examples

### Development Environment

For development, you may want to disable reduction or use more permissive settings:

```bash
ChatHistoryReduction__Enabled=true
ChatHistoryReduction__UseSummarization=false  # Use simple truncation for faster processing
ChatHistoryReduction__TargetCount=20
ChatHistoryReduction__ThresholdCount=40
ChatHistoryReduction__KeepLastMessages=15
```

### Production Environment

For production, use AI summarization for better context preservation:

```bash
ChatHistoryReduction__Enabled=true
ChatHistoryReduction__UseSummarization=true
ChatHistoryReduction__TargetCount=15
ChatHistoryReduction__ThresholdCount=30
ChatHistoryReduction__KeepLastMessages=10
ChatHistoryReduction__EstimatedTokensPerMessage=200
ChatHistoryReduction__MaxContextTokens=100000
```

## Deployment Integration

### Azure Container Apps (Bicep)

Add these environment variables to the Container App configuration in the Bicep template or parameter files:

```bicep
environmentVariables: [
  // ... existing variables ...
  {
    name: 'ChatHistoryReduction__Enabled'
    value: 'true'
  }
  {
    name: 'ChatHistoryReduction__UseSummarization'
    value: 'true'
  }
  {
    name: 'ChatHistoryReduction__TargetCount'
    value: '15'
  }
  {
    name: 'ChatHistoryReduction__ThresholdCount'
    value: '30'
  }
  {
    name: 'ChatHistoryReduction__KeepLastMessages'
    value: '10'
  }
]
```

### GitHub Actions / Azure DevOps Pipelines

Set these as secrets or variables in your CI/CD pipeline and reference them during deployment:

```yaml
# GitHub Actions example
env:
  CHAT_HISTORY_ENABLED: 'true'
  CHAT_HISTORY_USE_SUMMARIZATION: 'true'
  CHAT_HISTORY_TARGET_COUNT: '15'
  CHAT_HISTORY_THRESHOLD_COUNT: '30'
  CHAT_HISTORY_KEEP_LAST_MESSAGES: '10'
```

### Azure CLI Deployment

If deploying manually, set environment variables using Azure CLI:

```bash
az containerapp update \
  --name <app-name> \
  --resource-group <resource-group> \
  --set-env-vars \
    "ChatHistoryReduction__Enabled=true" \
    "ChatHistoryReduction__UseSummarization=true" \
    "ChatHistoryReduction__TargetCount=15" \
    "ChatHistoryReduction__ThresholdCount=30" \
    "ChatHistoryReduction__KeepLastMessages=10"
```

## Configuration Behavior

### How It Works

1. **Full History Storage**: The complete conversation history is always stored in Cosmos DB
2. **Selective Reduction**: Only the context sent to the AI model is reduced
3. **Threshold-Based Triggering**: Reduction only occurs when message count exceeds the threshold
4. **Preservation Strategy**:
   - Always keeps the system message (instructions)
   - Always keeps the last N messages (configurable via `KeepLastMessages`)
   - Summarizes or truncates older messages in the "middle band"

### Example Scenario

With default settings (`ThresholdCount=30`, `TargetCount=15`, `KeepLastMessages=10`):

- Messages 1-30: No reduction, full history sent to AI
- Messages 31+: Reduction triggered
  - System message: Preserved
  - Messages 1-20: Summarized into a single summary message (if `UseSummarization=true`)
  - Messages 21-31: Kept as-is (last 10 messages)
  - Result: System + Summary + Last 10 messages = ~12 messages sent to AI

## Monitoring

Monitor the effectiveness of chat history reduction through:

1. **Application Insights Logs**: Look for `[ChatHistoryReduction]` prefixed log messages
2. **Token Usage Metrics**: Track reduction in `[TokenMetrics]` logs
3. **Performance**: Monitor response times to ensure summarization doesn't add significant latency

## Troubleshooting

### Issue: Conversations losing context

**Solution**: Increase `KeepLastMessages` or `TargetCount` values

### Issue: High token usage/costs

**Solution**: Decrease `ThresholdCount` to trigger reduction earlier

### Issue: Summarization taking too long

**Solution**: Set `UseSummarization=false` to use fast truncation instead

### Issue: Reduction not triggering

**Solution**: Verify `Enabled=true` and check that conversation exceeds `ThresholdCount`

## Best Practices

1. **Start Conservative**: Begin with higher thresholds and gradually optimize
2. **Monitor Token Usage**: Track actual token consumption in Application Insights
3. **Test Summarization**: Verify that summaries preserve important context
4. **Environment-Specific**: Use different settings for dev/staging/prod
5. **Document Changes**: Keep track of configuration changes and their impact

## Related Configuration

Ensure these related settings are also configured:

- `AzureOpenAI__MaxTokens`: Maximum tokens for completion (affects reduction strategy)
- `AzureOpenAI__ChatDeploymentName`: Model used (affects token limits)
- `CosmosDb__Endpoint`: Required for full history storage

## Support

For issues or questions about chat history reduction:

1. Review Application Insights logs for `[ChatHistoryReduction]` entries
2. Check Cosmos DB for complete conversation history
3. Verify environment variables are set correctly in the Container App
4. Consult the `ChatHistoryReducerService.cs` implementation for details
