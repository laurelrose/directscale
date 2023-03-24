namespace WebExtension.Models.GenericReports
{
    public class MyItem
    {
        public string ID { get; set; }
        public string[] Links { get; set; }
        public ItemInput[] Inputs { get; set; }
        public MyItem[] Boxes { get; set; }
        public string QueryString { get; set; }
        public bool ShowColumns { get; set; }

        internal bool ContainsLink(string link)
        {
            foreach (string lnk in Links)
            {
                if (lnk == link) return true;
            }
            return false;
        }

        internal string GetInputValue(string id)
        {
            foreach (ItemInput input in Inputs)
            {
                if (input.ID == id) return input.Value;
            }
            return string.Empty;
        }
    }
}
