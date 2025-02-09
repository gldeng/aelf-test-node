using System.Collections.Generic;
using AElf.CrossChain;
using AElf.EconomicSystem;
using AElf.GovernmentSystem;
using AElf.Kernel.Configuration;
using AElf.Kernel.Consensus;
using AElf.Kernel.Proposal;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Types;

namespace AElf.Blockchains.SideChain;

extern alias CrossChainCore;

public class SideChainContractDeploymentListProvider : IContractDeploymentListProvider
{
    public List<Hash> GetDeployContractNameList()
    {
        return new List<Hash>
        {
            ProfitSmartContractAddressNameProvider.Name,
            TokenHolderSmartContractAddressNameProvider.Name,
            ConsensusSmartContractAddressNameProvider.Name,
            AssociationSmartContractAddressNameProvider.Name,
            ReferendumSmartContractAddressNameProvider.Name,
            ParliamentSmartContractAddressNameProvider.Name,
            TokenSmartContractAddressNameProvider.Name,
            CrossChainCore::AElf.CrossChain.CrossChainSmartContractAddressNameProvider.Name,
            ConfigurationSmartContractAddressNameProvider.Name
        };
    }
}