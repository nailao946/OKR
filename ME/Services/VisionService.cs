using ME.Data;
using ME.Models;

namespace ME.Services
{
    public class VisionService
    {
        private readonly VisionRepository _repo;

        public VisionService()
        {
            _repo = new VisionRepository();
        }

        public Vision GetLatestVision() => _repo.GetLatestVision();

        public int CreateVision(Vision vision) => _repo.InsertVision(vision);

        public void UpdateVision(Vision vision) => _repo.UpdateVision(vision);

        public Vision GetOrCreateVision()
        {
            var vision = _repo.GetLatestVision();
            if (vision == null)
            {
                vision = new Vision
                {
                    CareerVision = "",
                    FinanceVision = "",
                    HealthVision = "",
                    FamilyVision = "",
                    SocialVision = "",
                    LearningVision = "",
                    LeisureVision = "",
                    SpiritualVision = "",
                    LifeScene = ""
                };
                var id = _repo.InsertVision(vision);
                vision.Id = id;
            }
            return vision;
        }
    }
}
