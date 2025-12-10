# Chat History Reducer Implementation

## Overview

The chat history reducer feature manages conversation context window size by intelligently summarizing older messages while preserving recent interactions. This implementation follows Microsoft Semantic Kernel best practices and architectural guidelines.

## Architecture

### Core Principles

1. **Separation of Concerns**: The system maintains two distinct views of chat history:
   - **Full History**: Complete conversation stored in CosmosDB
   - **Reduced History**: Compressed view sent to the AI model

2. **Layered History Management**:
   - **Always Kept**: System message and last N messages (recent context)
   - **Summarized**: Middle-band older messages condensed into summary
   - **Archived**: Full history remains in persistent storage

3. **Threshold-Based Reduction**: Reduction only triggers when history exceeds configured thresholds, preventing unnecessary processing on every turn

4. **Token-Aware**: System tracks token usage and reserves safety margin for model context window

## Configuration

### Settings

Configure the reducer in `appsettings.json` under `AzureOpenAI`:

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

### Configuration Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `EnableChatHistoryReducer` | bool | true | Enable/disable the chat history reducer feature |
| `ChatHistoryReducerTargetCount` | int | 15 | Target number of messages to retain after reduction (excluding system message). Recommended: 10-20 messages. |
| `ChatHistoryReducerThresholdCount` | int | 5 | Number of messages beyond target that must be present before reduction triggers. Prevents reduction on every message. Recommended: 5-10 messages. |
| `ChatHistoryReducerUseSingleSummary` | bool | true | When true, maintains a single rolling summary that gets updated. When false, creates multiple summaries over time (may still exceed token limits). |

### How Thresholds Work

**Example**: Target=15, Threshold=5
- Reduction triggers when history reaches 20+ messages (15 + 5)
- After reduction, history contains ~15 messages
- This creates a buffer zone preventing constant summarization

## Implementation Details

### Integration Points

1. **AIService Constructor**: Initializes `ChatHistorySummarizationReducer` if enabled
2. **ProcessChatMessageAsync**: Applies reduction before sending to model, after adding user message
3. **CosmosDB Storage**: Saves complete, unreduced history for persistence

### Reduction Flow

```
1. User sends message
2. System retrieves full history from CosmosDB
3. User message added to full history
4. [Reduction Check] If history > (target + threshold):
   a. ChatHistorySummarizationReducer creates compressed view
   b. Logs reduction metrics
   c. Tracks in thought process
5. Send reduced (or full) history to AI model
6. Receive AI response
7. Add AI response to full history
8. Save complete history to CosmosDB
```

### Semantic Kernel Integration

Uses Microsoft's `ChatHistorySummarizationReducer`:
- Avoids orphaning function calls/results
- Attempts to keep user-assistant message pairs together
- Uses configurable summarization prompt
- Metadata marking for summary messages

## Benefits

1. **Cost Reduction**: Fewer tokens sent to model reduces API costs
2. **Performance**: Faster model responses with smaller context
3. **Context Preservation**: Full history always available for audit/replay
4. **Flexibility**: Configurable thresholds for different use cases

## Monitoring

### Logs

The system logs reduction events at INFO level:

```
[ChatHistoryReducer] History reduced for model input - ConversationId: {id}, 
  OriginalCount: 25, ReducedCount: 15, ReductionPercent: 40.0%
```

When reduction is not needed:

```
[ChatHistoryReducer] No reduction needed - ConversationId: {id}, MessageCount: 18
```

### Thought Process Tracking

Each reduction event is captured in the thought process with:
- Original message count
- Reduced message count
- Number of messages removed

## Best Practices

### Recommended Settings by Model

| Model | Context Window | Target Count | Threshold Count | Rationale |
|-------|----------------|--------------|-----------------|-----------|
| GPT-4o | 128k tokens | 15-20 | 5-10 | Large window allows more context |
| GPT-4 | 8k tokens | 10-15 | 3-5 | Smaller window needs aggressive reduction |
| GPT-3.5 | 4k tokens | 8-12 | 3-5 | Very limited window |

### Tuning Guidelines

1. **Target Count**:
   - Higher values preserve more context but use more tokens
   - Lower values reduce costs but may lose important context
   - Consider average conversation length

2. **Threshold Count**:
   - Higher thresholds reduce summarization frequency
   - Lower thresholds keep history tighter
   - Balance between cost and processing overhead

3. **Single Summary**:
   - Enable (true) for most use cases to prevent unbounded growth
   - Disable (false) only if preserving detailed historical context is critical

## Troubleshooting

### History Growing Too Large

**Symptom**: History exceeds token limits despite reducer

**Solutions**:
- Decrease `ChatHistoryReducerTargetCount`
- Decrease `ChatHistoryReducerThresholdCount`  
- Verify `ChatHistoryReducerUseSingleSummary` is true

### Too Much Context Lost

**Symptom**: AI lacks awareness of earlier conversation

**Solutions**:
- Increase `ChatHistoryReducerTargetCount`
- Increase `ChatHistoryReducerThresholdCount`
- Review summarization quality in logs

### Frequent Summarization Overhead

**Symptom**: Performance degradation, frequent summarization logs

**Solutions**:
- Increase `ChatHistoryReducerThresholdCount` to create larger buffer
- Consider conversation patterns and adjust target accordingly

## Testing

### Unit Tests

Tests verify:
- Reducer initialization when enabled/disabled
- Various configuration value combinations
- Constructor behavior with different settings

Run tests:
```bash
dotnet test --filter "FullyQualifiedName~ChatHistoryReducerTests"
```

### Integration Testing

To test reducer behavior:
1. Configure low threshold (e.g., target=3, threshold=2)
2. Have conversation with 10+ messages
3. Observe reduction logs
4. Verify full history in CosmosDB
5. Confirm model receives reduced history

## References

- [Semantic Kernel Chat History Documentation](https://learn.microsoft.com/en-us/semantic-kernel/concepts/ai-services/chat-completion/chat-history)
- [Chat History Reducer Interface](https://github.com/microsoft/semantic-kernel/blob/main/dotnet/src/SemanticKernel.Abstractions/AI/ChatCompletion/IChatHistoryReducer.cs)
- [ChatHistorySummarizationReducer Implementation](https://github.com/microsoft/semantic-kernel/blob/main/dotnet/src/SemanticKernel.Core/AI/ChatCompletion/ChatHistorySummarizationReducer.cs)

## Version History

- **1.0.0** (2025-12-09): Initial implementation with Semantic Kernel 1.68.0
  - Configurable target and threshold counts
  - Single vs multiple summary modes
  - Full CosmosDB persistence
  - Comprehensive logging and monitoring
