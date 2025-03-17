using System;
using System.Collections.Generic;
using System.Linq;
using ModularFramework.Utility;

[Serializable]
public class InkLine {
    public List<string> characters = new();
    public string text;
    public string subText;
    public List<InkTag> tags;
    public bool hide; // In line, it means disguise character info. In Choice, it means choice unselecteable. Not preset at definition



    public InkLine(string text, List<InkTag> tags) {
        this.text = text;
        if(tags == null) this.tags =  new();
        else {
            bool isConditionSet = false;
            this.tags = tags.Where(t => {
                if(t.type == InkTagType.CONDITION) {
                    if(isConditionSet) {
                        DebugUtil.Error("Can not have more than one condition in " + text);
                    }
                    hide = !bool.Parse(t.codes[0]);
                    subText = t.codes[1];
                    isConditionSet = true;
                    return false;
                }
                if(t.type == InkTagType.CHARACTER) {
                    characters.Add(t.codes[0]);
                    return false;
                }
                return true;
            }).ToList();
        }

    }
}