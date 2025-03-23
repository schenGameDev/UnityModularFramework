A Client-server framework for unity. 

Each scene is treated as a frontend, which contains simple in-scene gameObject-to-gameObject logic.

All complex logics are handled in backend, by GameModules and GameSystems (ScriptableObject). 
Most data is stored in backend and sends to frontend at runtime.

Backend-frontend communication is realized via EventChannel. All EventChannels are gated by one-and-only Singleton in scene, GameRunner. 

GameModules join the player loop via GameRunner. They keep track of objects in scene, calculate, and give instructions, etc.

GameSystems are pure backend classes which can be called by GameModules or frontend via EventChannel for specific purpose.

(Libraries are not uploaded)
