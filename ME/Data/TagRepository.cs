using System;
using System.Collections.Generic;
using System.Linq;
using ME.Models;

namespace ME.Data
{
    public class TagRepository
    {
        private const string FileName = "tags";

        public List<GoalTag> GetAllTags()
        {
            return JsonStore.Load<GoalTag>(FileName).OrderBy(t => t.SortOrder).ToList();
        }

        public GoalTag GetTagById(int id)
        {
            return JsonStore.Load<GoalTag>(FileName).FirstOrDefault(t => t.Id == id);
        }

        public int InsertTag(GoalTag tag)
        {
            var tags = JsonStore.Load<GoalTag>(FileName);
            var maxId = tags.Count > 0 ? tags.Max(t => t.Id) : 0;
            tag.Id = maxId + 1;
            tag.CreatedAt = tag.CreatedAt == default ? DateTime.Now : tag.CreatedAt;
            tag.SortOrder = tag.SortOrder == 0 ? tags.Count : tag.SortOrder;
            tags.Add(tag);
            JsonStore.Save(FileName, tags);
            return tag.Id;
        }

        public void UpdateTag(GoalTag tag)
        {
            var tags = JsonStore.Load<GoalTag>(FileName);
            var index = tags.FindIndex(t => t.Id == tag.Id);
            if (index >= 0)
            {
                tags[index] = tag;
                JsonStore.Save(FileName, tags);
            }
        }

        public void DeleteTag(int id)
        {
            var tags = JsonStore.Load<GoalTag>(FileName);
            tags.RemoveAll(t => t.Id == id);
            JsonStore.Save(FileName, tags);
        }
    }
}
