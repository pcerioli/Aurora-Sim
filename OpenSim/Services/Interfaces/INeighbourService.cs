/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using OpenSim.Framework;
using OpenMetaverse;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace OpenSim.Services.Interfaces
{
    public interface INeighbourService
    {
        /// <summary>
        /// Neighbors of all local regions
        /// UUID - RegionID
        /// List<GridRegion> - Neighbors of the given region
        /// </summary>
        Dictionary<UUID, List<GridRegion>> Neighbors { get; }
        
        /// <summary>
        /// Tell the neighbors that this region is now up and running
        /// </summary>
        /// <param name="incomingRegion">The region that is now up</param>
        /// <returns>Returns the list of neighbors that were informed of this change</returns>
        List<GridRegion> InformNeighborsThatRegionisUp(RegionInfo incomingRegion);
        
        /// <summary>
        /// Send a child agent update to the neighbors
        /// </summary>
        /// <param name="childAgentUpdate">The update to send</param>
        /// <param name="regionID">The region the client is currently in</param>
        void SendChildAgentUpdate(AgentPosition childAgentUpdate, UUID regionID);

        /// <summary>
        /// Close the given agent in all of it's regions
        /// </summary>
        /// <param name="agentID">The client's UUID</param>
        /// <param name="regionID">The region the client is currently in</param>
        /// <param name="regionsToClose">Regions in which to close the child agents</param>
        void SendCloseChildAgent(UUID agentID, UUID regionID, List<ulong> regionsToClose);
    }
}
