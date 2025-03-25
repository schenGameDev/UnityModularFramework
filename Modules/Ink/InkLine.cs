using System;
using System.Collections.Generic;
using System.Linq;
using ModularFramework.Utility;

[Serializable]
public class InkLine {
    public List<string> characters = new();
    public string dialogBoxId;
    public string text;
    public string subText;
    public List<InkTag> tags;
    public bool hide; // In line, it means disguise character info. In Choice, it means choice unselecteable. Not preset at definition

    public string portraitId;
    public string portraitPosition;


    public InkLine(string text, List<InkTag> tags, bool inChoice = false) {
        this.text = text;
        if(tags == null) this.tags =  new();
        else {
            bool isConditionSet = false;
            bool isDialogBoxSet = false;
            this.tags = tags.Where(t => {
                if(t.type == InkTagType.CONDITION) {
                    if(isConditionSet) {
                        DebugUtil.Error("Can not have more than one condition in " + text);
                        return false;
                    }
                    hide = !bool.Parse(t.codes[0]);
                    subText = t.codes[1]; // true condition expression
                    isConditionSet = true;
                    return false;
                }
                if(t.type == InkTagType.CHARACTER) {
                    characters.Add(t.codes[0]);
                    return false;
                }
                if(!inChoice && t.type == InkTagType.GROUP) {
                    if(isDialogBoxSet) {
                        DebugUtil.Error("Can not have more than one group in " + text);
                        return false;
                    }
                    dialogBoxId = t.codes[0];
                    isDialogBoxSet = true;
                    return false;
                }
                if(!inChoice && t.type == InkTagType.PORTRAIT) {
                    portraitId = t.codes[0];
                    portraitPosition = t.codes[1];
                    return false;
                }
                return true;
            }).ToList();
        }

    }
}