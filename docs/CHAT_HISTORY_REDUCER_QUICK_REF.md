# Chat History Reducer - Quick Reference

## Overview
Intelligent chat history management using Semantic Kernel's ChatHistorySummarizationReducer to optimize token usage while preserving conversation context.

## Quick Start

### Configuration (appSettings.json)
```json
{
  "AzureOpenAI": {
    "EnableChatHistoryReducer": true,
    "ChatHistoryReducerTargetCount": 15,
    "ChatHistoryReducerThresholdCount": 5,
    "ChatHistoryReducerUseSingleSummary": true
  }
}
```

### How It Works
- **Full history** → Always stored in CosmosDB
- **Reduced history** → Sent to AI model when count > (target + threshold)
- **Reduction** → Summarizes middle messages, keeps system + recent N messages

### Example
- Target: 15, Threshold: 5
- Reduction triggers at 20+ messages
- After reduction: ~15 messages sent to model
- Full 20+ messages still in CosmosDB ✓

## Key Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `EnableChatHistoryReducer` | true | Enable/disable feature |
| `ChatHistoryReducerTargetCount` | 15 | Target message count after reduction |
| `ChatHistoryReducerThresholdCount` | 5 | Buffer before triggering reduction |
| `ChatHistoryReducerUseSingleSummary` | true | Single rolling summary vs multiple |

## Benefits
- 💰 **Lower costs**: Fewer tokens to model
- ⚡ **Faster responses**: Smaller context
- 📦 **Full history**: Always preserved in storage
- 📊 **Monitoring**: Comprehensive logging

## Monitoring

Look for logs like:
```
[ChatHistoryReducer] History reduced for model input
  ConversationId: abc-123, OriginalCount: 25, 
  ReducedCount: 15, ReductionPercent: 40.0%
```

## Recommended Settings by Model

| Model | Target Count | Threshold Count |
|-------|--------------|-----------------|
| GPT-4o (128k) | 15-20 | 5-10 |
| GPT-4 (8k) | 10-15 | 3-5 |
| GPT-3.5 (4k) | 8-12 | 3-5 |

## Troubleshooting

### History too large?
- Decrease `TargetCount` or `ThresholdCount`
- Verify `UseSingleSummary` is true

### Too much context lost?
- Increase `TargetCount` or `ThresholdCount`

### Frequent summarization?
- Increase `ThresholdCount` for larger buffer

## Architecture

```
┌─────────────────┐
│  User Message   │
└────────┬────────┘
         │
         ↓
┌─────────────────┐
│ Full History    │ ← Retrieved from CosmosDB
│ (All messages)  │
└────────┬────────┘
         │
         ↓
    [Reduction?]
    Yes if > 20
         │
         ↓
┌─────────────────┐
│ Reduced View    │ ← System + Recent + Summary
│ (15 messages)   │
└────────┬────────┘
         │
         ↓
┌─────────────────┐
│   AI Model      │
└────────┬────────┘
         │
         ↓
┌─────────────────┐
│ Save Full       │ ← Full history to CosmosDB
│ History         │
└─────────────────┘
```

## See Also
- Full documentation: `docs/CHAT_HISTORY_REDUCER.md`
- Tests: `AzureDevOpsAI.Backend.Tests/Services/ChatHistoryReducerTests.cs`
- Implementation: `AzureDevOpsAI.Backend/Services/AIService.cs`
