using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace PETools
{
    public class PESectionList
    {
        OrderedDictionary sectionDict;

        public PESectionList()
        {
            sectionDict = new OrderedDictionary();
        }

        public void Add(PESection section)
        {
            string groupName = section.Name;
            if (groupName.Contains("$"))
            {
                groupName = groupName.Substring(0, groupName.IndexOf('$'));
            }

            ArrayList sectionGroup = null;
            if (sectionDict.Contains(groupName))
            {
                sectionGroup = (ArrayList)sectionDict[groupName];
            }
            else
            {
                sectionGroup = new ArrayList();
                sectionDict.Add(groupName, sectionGroup);
            }

            int index = 0;
            foreach (PESection s in sectionGroup)
            {
                if (PESection.Compare(section, s) < 0)
                {
                    sectionGroup.Insert(index, section);
                    return;
                }
                index++;
            }

            sectionGroup.Add(section);
        }

        public void AddRange(List<PESection> range)
        {
            foreach (PESection s in range)
                this.Add(s);
        }

        public List<PESection> MergeSections()
        {
            List<PESection> mergedSections = new List<PESection>();

            IDictionaryEnumerator group = sectionDict.GetEnumerator();
            String str = String.Empty;
            while (group.MoveNext())
            {
                PESection s = MergeSectionGroup((ArrayList)group.Value);
                s.Name = (String)group.Key;
                mergedSections.Add(s);
            }

            return mergedSections;
        }

        PESection MergeSectionGroup(ArrayList group)
        {
            // Create new section, copy the header from the first
            // section to get us started.
            PESection section = new PESection(((PESection)group[0]).Header);

            // Calculate total raw section size
            int sectionSize = 0;
            foreach (PESection s in group)
                sectionSize += s.Data.Length;

            byte[] sectionData = new byte[sectionSize];
            int offset = 0;
            foreach (PESection s in group)
            {
                Array.Copy(s.Data, 0, sectionData, offset, s.Data.Length);
                offset += s.Data.Length;
            }
            section.Data = sectionData;
            section.RawSize = (uint)sectionSize;
            section.PhysicalAddress = 0;

            return section;
        }

        public override string ToString()
        {
            IDictionaryEnumerator group = sectionDict.GetEnumerator();
            String str = String.Empty;
            while (group.MoveNext())
            {
                str += String.Format("Section Group: {0}:\n", group.Key);
                ArrayList sections = (ArrayList)group.Value;
                foreach (PESection section in sections)
                {
                    str += String.Format("\tSource: {0}\t\tSection: {1}\n",
                        section.SourceCoff.SourceFile, section.Name);
                }
            }
            return str;
        }
    }
}
