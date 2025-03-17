using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class InkChoice {
    public List<InkLine> choices;
    /// <summary>
    /// the UI button group responsible for display
    /// </summary>
    public string group;


    public InkChoice(List<InkLine> choices, Func<string,string> explainCondition) {
        this.choices = choices;
        group = choices.SelectMany(c=>c.tags.Where(t=>t.type==InkTagType.CHOICE_GROUP)
                                .Select(t=>t.codes[0])).FirstOrDefault();
        choices.ForEach(c => {
            c.tags.RemoveWhere(t=>t.type==InkTagType.CHOICE_GROUP);
            c.subText = explainCondition(c.subText);
        });
    }

}