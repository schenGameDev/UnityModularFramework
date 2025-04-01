CONST CHANGE_SCENE = "changeScene"
CONST PLAY_SOUND = "playSound"
CONST PLAY_BGM = "playBGM"
CONST PLAY_CG = "playCG"
CONST ADD_QUEST = "addQuest"
CONST DROP_QUEST = "dropQuest"
CONST COMPLETE_QUEST = "completeQuest"
CONST ADD_NOTE = "addNote"

VAR LoopNum = 1
VAR San = 60
VAR Health = 100
VAR HaveKeyToOffice = false
VAR HaveFlashlight = false


// internal function
=== function scene(sceneName)
    ~ doTask(CHANGE_SCENE, sceneName, true)

=== function sound(soundName)
    ~ doTask(PLAY_SOUND, soundName, false)

=== function bgm(bgmName)
    ~ doTask(PLAY_BGM, bgmName, false)
    
=== function cg(cgName)
    ~ doTask(PLAY_CG, cgName, true)
    
=== function quest(questName)
    ~ doTask(ADD_QUEST, questName, true)

=== function dropQuest(questName)
    ~ doTask(DROP_QUEST, questName, true)
    
=== function completeQuest(questName)
    ~ doTask(COMPLETE_QUEST, questName, true)    
    
=== function note(notes)
    ~ doTask(ADD_NOTE, notes, true)

// Unity function
EXTERNAL doTask(taskName, param, isBlocking)
