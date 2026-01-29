# OctoLearn Question Integration Setup Guide

This guide explains how to set up the integration between OctoLearn authentication and the DontDestroy_ResearchManager_LvlUnlocker_Questionpuffer system in the MainMenuScene.

## Components Overview

The integration consists of three main components:

1. **QuestionIntegrationManager** - Core logic to load questions from OctoLearn and manage the question buffer
2. **QuestionUIManager** - UI handling for displaying questions and collecting answers
3. **MainMenuQuestionInitializer** - Setup and initialization in the MainMenuScene

## Setup Instructions

### 1. Create the DontDestroy GameObject

In your MainMenuScene, create a new GameObject named:
```
DontDestroy_ResearchManager_LvlUnlocker_Questionpuffer
```

### 2. Add the QuestionIntegrationManager

Add the `QuestionIntegrationManager` component to the DontDestroy GameObject.

Configuration options:
- **DontDestroyContainer**: Reference to the container GameObject (itself)
- **Number of Questions to Load**: How many questions to fetch at once (default: 5)
- **Load Questions on Start**: Whether to automatically load questions on startup
- **Research Points Per Correct Answer**: How many research points to award per correct answer

### 3. Create the Question UI

1. Create a UI Panel for displaying questions (you can use any design)
2. Add the following UI elements:
   - Text for displaying the question
   - Input field for the answer
   - Container for multiple choice options
   - Submit button
   - Close button
   - Feedback text

3. Add the `QuestionUIManager` component to the panel
4. Configure the references to all UI elements

### 4. Set Up the Initializer

1. Create a new GameObject in your MainMenuScene
2. Add the `MainMenuQuestionInitializer` component
3. Configure the references:
   - **DontDestroyContainer**: Reference to the DontDestroy GameObject
   - **QuestionPrefab**: Reference to a prefab containing the question UI (optional)
   - **QuestionPrefabParent**: Where to instantiate the UI prefab (if using)
   - **Get Questions Button**: Button to trigger question loading
   - **Show Question UI Button**: Button to display the question UI

### 5. Ensure Authentication is Set Up

Make sure you have properly set up the OctoLearn authentication system:

1. Ensure OctoAuthManager and OctoApiService are present
2. Make sure users can log in through the OctoLearn authentication flow

## Integration with Research System

The QuestionIntegrationManager automatically integrates with the ResearchManager:

1. When users answer questions correctly, they earn research points
2. The ResearchManager uses these points for tower and upgrade research
3. All question data is maintained across scene changes via DontDestroyOnLoad

## Integration with Level Unlocker

This system is compatible with the LevelUnlocker:

1. Questions loaded from OctoLearn can be used to determine level unlocking
2. Level progress is saved alongside question data

## Customization

You can extend this system by:

1. Creating custom question display formats
2. Adding additional reward systems
3. Implementing specific learning paths tied to game progression

## Troubleshooting

- If questions aren't loading, ensure the user is authenticated with OctoLearn
- If research points aren't being awarded, check that ResearchManager is properly initialized
- If the UI doesn't appear, verify all references are set correctly 