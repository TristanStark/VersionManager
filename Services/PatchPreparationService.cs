using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VersionManager.Services.Interfaces;

namespace VersionManager.Services
{
    public class PatchPreparationService : IPatchPreparationService
    {
        public Task PreparePatchAsync(string rawPatchFolder, string preparedPatchFolder)
        {
            return Task.CompletedTask;
        }

    }
}
