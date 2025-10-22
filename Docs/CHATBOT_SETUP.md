# Travel Tracker Chatbot Setup Guide

## Overview

The Travel Tracker chatbot is an AI-powered assistant that helps users interact with their travel data using natural language. It integrates with Azure AI Foundry (AI Studio) and can answer questions about locations, national parks, and travel statistics.

## Features

- 🤖 Natural language conversation interface
- 📍 Query locations by state, date range, or view all
- 📊 Get travel statistics (states visited, location counts)
- 🏞️ Information about US National Parks
- 🏷️ Available location types
- 🔐 Secure, user-specific data access

## Prerequisites

1. **Azure Subscription** - You need an active Azure subscription
2. **Azure AI Foundry Project** - Create a project in Azure AI Foundry (formerly Azure AI Studio)
3. **LLM Deployment** - Deploy a language model (e.g., GPT-4, GPT-3.5-turbo) in your AI Foundry project

## Configuration Steps

### 1. Create Azure AI Foundry Resources

1. Go to [Azure AI Foundry](https://ai.azure.com/)
2. Create a new project or use an existing one
3. Deploy a language model (recommended: GPT-4 or GPT-3.5-turbo)
4. Note the following information:
   - **Endpoint URL** - Your AI Foundry endpoint (e.g., `https://your-project.openai.azure.com/`)
   - **API Key** - Your authentication key
   - **Deployment Name** - The name you gave to your model deployment

### 2. Configure Application Settings

Update the `appsettings.json` file in the `src/TravelTracker` directory:

```json
{
  "AzureAIFoundry": {
    "Endpoint": "https://your-project.openai.azure.com/",
    "ApiKey": "your-api-key-here",
    "DeploymentName": "your-deployment-name"
  }
}
```

**For production environments**, use **Azure Key Vault** or **User Secrets** to store sensitive information:

```bash
cd src/TravelTracker
dotnet user-secrets set "AzureAIFoundry:Endpoint" "https://your-project.openai.azure.com/"
dotnet user-secrets set "AzureAIFoundry:ApiKey" "your-api-key-here"
dotnet user-secrets set "AzureAIFoundry:DeploymentName" "your-deployment-name"
```

### 3. Run the Application

```bash
cd src/TravelTracker
dotnet run
```

Navigate to the Chat page from the navigation menu (🤖 Chat).

## Using the Chatbot

### Example Questions

The chatbot can understand various types of questions:

**Locations:**
- "What locations have I visited?"
- "Show me all my locations"
- "What places have I been to in California?"
- "List my visits in Texas"

**Statistics:**
- "How many states have I visited?"
- "What are my travel statistics?"
- "How many locations do I have?"
- "Which states have I visited the most?"

**Date Ranges:**
- "What locations did I visit in 2024?"
- "Show me places I visited last year"
- "Where did I go between January and March?"

**National Parks:**
- "Tell me about national parks"
- "What national parks are in California?"
- "List all national parks"

**Location Types:**
- "What types of locations can I track?"
- "What location categories are available?"

### Chat Interface

The chat interface includes:
- **Message History** - Shows conversation between you and the assistant
- **Input Field** - Type your message here
- **Send Button** - Submit your message (or press Enter)
- **Loading Indicator** - Shows when the assistant is thinking
- **Welcome Message** - Provides suggestions for getting started

## How It Works

### Context Gathering

The chatbot uses intelligent query analysis to determine what information to fetch:

1. **Pattern Matching** - Analyzes your message using regex patterns
2. **API Calls** - Fetches relevant data from the Travel Tracker APIs
3. **Context Building** - Prepares data in a format the LLM can understand
4. **LLM Processing** - Sends your question + context to Azure AI Foundry
5. **Natural Response** - Receives and displays a conversational answer

### Data Privacy

- ✅ Only your own travel data is accessible
- ✅ User authentication is required
- ✅ Data stays within your Azure subscription
- ✅ No data is shared with third parties

## Architecture

```
User Input
    ↓
ChatbotController (API)
    ↓
ChatbotService
    ├── Analyzes query intent
    ├── Fetches relevant data from APIs
    │   ├── LocationService
    │   ├── NationalParkService
    │   └── LocationTypeService
    ├── Builds context for LLM
    └── Calls Azure AI Foundry
    ↓
Azure AI Foundry (LLM)
    ↓
Natural Language Response
```

## API Endpoints

### POST /api/chatbot/message

Send a message to the chatbot.

**Request Body:**
```json
{
  "message": "What locations have I visited in California?"
}
```

**Response:**
```json
{
  "message": "You have visited 5 locations in California: ...",
  "timestamp": "2025-10-22T18:00:00Z"
}
```

## Troubleshooting

### "Chatbot is not configured"

This message appears when Azure AI Foundry settings are missing or incomplete. Check:
- All three settings (Endpoint, ApiKey, DeploymentName) are configured
- The endpoint URL is correct and accessible
- The API key is valid

### Slow Responses

- LLM processing can take a few seconds
- Check your Azure AI Foundry deployment's performance tier
- Consider using a faster model for real-time chat

### "User not authenticated"

- Ensure you're logged in to the application
- Check Azure AD configuration in appsettings.json

## Cost Considerations

- Azure AI Foundry charges per token (input + output)
- The chatbot optimizes context to minimize token usage
- Monitor usage in Azure Portal → Azure AI Foundry → Usage

**Cost Optimization Tips:**
- Use GPT-3.5-turbo for lower costs
- Enable usage monitoring and set alerts
- Consider implementing rate limiting for production

## Security Best Practices

1. ✅ **Never commit API keys** to source control
2. ✅ Use Azure Key Vault for production secrets
3. ✅ Enable Azure AD authentication
4. ✅ Implement rate limiting on the chatbot endpoint
5. ✅ Monitor API usage for anomalies
6. ✅ Keep the Azure.AI.Inference SDK updated

## Support

For issues or questions:
- Check [Azure AI Foundry Documentation](https://learn.microsoft.com/azure/ai-studio/)
- Review application logs in the console
- See the main [README.md](../README.md) for general setup

## Future Enhancements

Potential improvements for the chatbot:
- [ ] Function calling (tool use) when Azure SDK supports it
- [ ] Conversation history persistence
- [ ] Multi-turn conversations with context
- [ ] Export chat transcripts
- [ ] Voice input/output
- [ ] Suggestions based on travel patterns
