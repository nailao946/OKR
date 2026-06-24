using System.Collections.Generic;
using System.Linq;
using ME.Models;

namespace ME.Data
{
    public class TimeTagRepository
    {
        private const string FileName = "time_tags";

        public List<TimeTag> GetAllTags()
        {
            var tags = JsonStore.Load<TimeTag>(FileName);
            if (tags.Count == 0)
            {
                tags.Add(new TimeTag { Id = 1, Name = "闲时", Color = "#808080", Notes = "空闲时间段", SortOrder = 0, IsPreset = true });
                tags.Add(new TimeTag { Id = 2, Name = "工作", Color = "#007AFF", Notes = "工作时间", SortOrder = 1, IsPreset = true });
                tags.Add(new TimeTag { Id = 3, Name = "休息", Color = "#34C759", Notes = "休息时间", SortOrder = 2, IsPreset = true });
                JsonStore.Save(FileName, tags);
            }
            return tags.OrderBy(t => t.SortOrder).ToList();
        }

        public TimeTag GetTagById(int id)
        {
            return GetAllTags().FirstOrDefault(t => t.Id == id);
        }

        public int InsertTag(TimeTag tag)
        {
            var tags = JsonStore.Load<TimeTag>(FileName);
            tag.Id = tags.Count > 0 ? tags.Max(t => t.Id) + 1 : 1;
            tags.Add(tag);
            JsonStore.Save(FileName, tags);
            return tag.Id;
        }

        public void UpdateTag(TimeTag tag)
        {
            var tags = JsonStore.Load<TimeTag>(FileName);
            var index = tags.FindIndex(t => t.Id == tag.Id);
            if (index >= 0)
            {
                tags[index] = tag;
                JsonStore.Save(FileName, tags);
            }
        }

        public void DeleteTag(int id)
        {
            var tags = JsonStore.Load<TimeTag>(FileName);
            tags.RemoveAll(t => t.Id == id && !t.IsPreset);
            JsonStore.Save(FileName, tags);
        }
    }
}
