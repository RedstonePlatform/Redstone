using NBitcoin;

namespace Redstone.Core.Networks.Deployments
{
    /// <summary>
    /// BIP9 deployments for the Stratis network.
    /// </summary>
    public class RedstoneBIP9Deployments : BIP9DeploymentsArray
    {
        // The position of each deployment in the deployments array.
        public const int TestDummy = 0;

        // The number of deployments.
        public const int NumberOfDeployments = 1;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedstoneBIP9Deployments"/> class.
        /// Constructs the BIP9 deployments array.
        /// </summary>
        public RedstoneBIP9Deployments() : base(NumberOfDeployments)
        {
        }

        /// <summary>
        /// Gets the deployment flags to set when the deployment activates.
        /// </summary>
        /// <param name="deployment">The deployment number.</param>
        /// <returns>The deployment flags.</returns>
        public override BIP9DeploymentFlags GetFlags(int deployment)
        {
            return new BIP9DeploymentFlags();
        }
    }
}

