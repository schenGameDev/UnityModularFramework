A Client-server framework for unity. 

Each scene is treated as a frontend, which contains simple in-scene gameObject-to-gameObject logic.

All complex logics are handled in backend, by GameModules (ScriptableObject). Most data is stored in backend and sends to frontend at runtime.

Backend-frontend communication is gated by one-and-only Singleton in scene, GameRunner. 
