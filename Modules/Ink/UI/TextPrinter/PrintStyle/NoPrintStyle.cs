namespace ModularFramework.Modules.Ink
{
    public class NoPrintStyle : PrintStyleBase
    {
        public override void Print(string text)
        {
            text = Prepare(text);
            if (Printer.endIndicator) Printer.endIndicator.SetActive(false);
            Finish(text);
            OnPrintComplete?.Invoke();
        }

        public override void Skip()
        {
        }

        public override void Destroy()
        {
        }

    }
}