
namespace ModIOBrowser
{
    /// <summary>
    /// Supporting struct for holding tag name along with their corresponding category name to be
    /// used inside of a HashSet.
    /// </summary>
    internal struct Tag
    {
        public string category;
        public string name;
        
        // Start is called before the first frame update
        public Tag(string category, string name)
        {
            this.category = category;
            this.name = name;
        }

        public override string ToString()
        {
            return $"{category}: {name}";
        }

        public override bool Equals(object obj)
        {
            if(obj is Tag tag)
            {
                return tag.category == category && tag.name == name;
            }
            
            return false;
        }

        // Update is called once per frame
        public override int GetHashCode()
        {
            return (name + category).GetHashCode();
        }
    }
}
