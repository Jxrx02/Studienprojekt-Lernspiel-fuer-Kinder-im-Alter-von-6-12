This asset contains multiple ase files for diferent type of actions. The legs aseprite file can be used in case of need to animate other actions while moving, you can try to programing it by yourself but there's a already working version as example in the unity package file (AnimationEventHandler.cs) that works by checking if the character is moving and performing a action and then deactivate the default leg layer (of the game object in runtime context) in the main aseprite file and active the object with the leg animator, the anim played also considers the diference of movement in relation to the direction the char is looking.

*** TO USE THE AUTO AIM ADD A LAYER NAMED "Target" ON THE UNITY LAYER LIST ***

The unity package file is available to download at the same link of the asset!