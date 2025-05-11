using System;
using System.Collections.Generic;
using System.Linq;
using ModularFramework.Utility;

[Serializable]
public class InkLine {
    public string character;
    public string dialogBoxId;
    public string dialogBoxSubId;
    public string text;
    public string subText;
    public List<InkTag> tags;
    public bool dialogue; // someone's speech
    public bool hide; // In line, it means disguise character info. In Choice, it means choice unselecteable. Not preset at definition
    public bool interrupted; // line finish early

    public string portraitId;
    public string portraitPosition;

    public int index = -1; // override
    public bool IsIndexOverriden => index != -1;


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
                if(t.type == InkTagType.CHARACTER)
                {
                    dialogue = true;
                    character =t.codes[0];
                    hide = t.codes[1] != "true"; 
                    return false;
                }
                if(!inChoice && t.type == InkTagType.GROUP) {
                    if(isDialogBoxSet) {
                        DebugUtil.Error("Can not have more than one group in " + text);
                        return false;
                    }
                    dialogBoxId = t.codes[0];
                    if(t.codes.Length > 1) dialogBoxSubId = t.codes[1];
                    isDialogBoxSet = true;
                    return false;
                }

                if (inChoice && t.type == InkTagType.INDEX)
                {
                    index = int.Parse(t.codes[0]);
                    return false;
                }

                if (inChoice && t.type == InkTagType.HIDE_CHOICE_TEXT)
                {
                    this.text = "";
                    return false;
                }
                
                if(!inChoice && t.type == InkTagType.INTERRUPTED)
                {
                    interrupted = true;
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