using DirectScale.Disco.Extension.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebExtension.Models;
using WebExtension.Repositories;

namespace WebExtension.Services
{
    public interface IAssociateWebService
    {
    }
    public class AssociateWebService : IAssociateWebService
    {
        private readonly IAssociateWebRepository _associateWebRepository;
        private readonly IAssociateService _associateService;
        public AssociateWebService(IAssociateService associateService, IAssociateWebRepository associateWebRepository)
        {
            _associateWebRepository = associateWebRepository ?? throw new ArgumentNullException(nameof(associateWebRepository));
            _associateService = associateService ?? throw new ArgumentNullException(nameof(associateService));
        }
    }
}
