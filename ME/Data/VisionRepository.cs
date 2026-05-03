using System;
using System.Collections.Generic;
using System.Linq;
using ME.Models;

namespace ME.Data
{
    public class VisionRepository
    {
        private const string FileName = "visions";

        public Vision GetLatestVision()
        {
            return JsonStore.Load<Vision>(FileName)
                .OrderByDescending(v => v.UpdatedAt).FirstOrDefault();
        }

        public int InsertVision(Vision vision)
        {
            var visions = JsonStore.Load<Vision>(FileName);
            var maxId = visions.Count > 0 ? visions.Max(v => v.Id) : 0;
            vision.Id = maxId + 1;
            vision.CreatedAt = DateTime.Now;
            vision.UpdatedAt = DateTime.Now;
            visions.Add(vision);
            JsonStore.Save(FileName, visions);
            return vision.Id;
        }

        public void UpdateVision(Vision vision)
        {
            var visions = JsonStore.Load<Vision>(FileName);
            var index = visions.FindIndex(v => v.Id == vision.Id);
            if (index >= 0)
            {
                vision.UpdatedAt = DateTime.Now;
                visions[index] = vision;
                JsonStore.Save(FileName, visions);
            }
        }
    }
}
