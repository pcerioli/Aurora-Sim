/*
 * Copyright (c) Contributors, http://aurora-sim.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the Aurora-Sim Project nor the
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
using Aurora.Framework;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;
using RegionFlags = Aurora.Framework.RegionFlags;

namespace Aurora.Services.DataService
{
    public class LocalGridConnector : IRegionData
    {
        private IGenericData GD;
        private string m_realm = "gridregions";

        #region IRegionData Members

        public void Initialize(IGenericData GenericData, IConfigSource source, IRegistryCore simBase,
                               string defaultConnectionString)
        {
            if (source.Configs["AuroraConnectors"].GetString("AbuseReportsConnector", "LocalConnector") ==
                "LocalConnector")
            {
                GD = GenericData;

                string connectionString = defaultConnectionString;
                if (source.Configs[Name] != null)
                    connectionString = source.Configs[Name].GetString("ConnectionString", defaultConnectionString);

                GD.ConnectToDatabase(connectionString, "GridRegions",
                                     source.Configs["AuroraConnectors"].GetBoolean("ValidateTables", true));

                DataManager.DataManager.RegisterPlugin(this);
            }
        }

        public string Name
        {
            get { return "IRegionData"; }
        }

        public List<GridRegion> Get(string regionName, UUID scopeID)
        {
            QueryFilter filter = new QueryFilter();
            filter.andLikeFilters["RegionName"] = regionName.MySqlEscape();
            if (scopeID != UUID.Zero)
            {
                filter.andFilters["ScopeID"] = scopeID;
            }

            List<string> query = GD.Query(new string[1] { "*" }, m_realm, filter, null, null, null);

            return (query.Count == 0) ? null : ParseQuery(query);
        }

        public List<GridRegion> Get(RegionFlags flags)
        {
            QueryFilter filter = new QueryFilter();
            filter.andBitfieldAndFilters["Flags"] = (uint)flags;
            return ParseQuery(GD.Query(new string[1] { "*" }, m_realm, filter, null, null, null));
        }

        public GridRegion Get(int posX, int posY, UUID scopeID)
        {
            QueryFilter filter = new QueryFilter();
            filter.andFilters["LocX"] = posX;
            filter.andFilters["LocY"] = posY;
            if (scopeID != UUID.Zero){
                filter.andFilters["ScopeID"] = scopeID;
            }

            List<string> query = GD.Query(new string[1] { "*" }, m_realm, filter, null, null, null);

            return (query.Count == 0) ? null : ParseQuery(query)[0];
        }

        public GridRegion Get(UUID regionID, UUID scopeID)
        {
            List<string> query;
            Dictionary<string, object> where = new Dictionary<string, object>();

            where["RegionUUID"] = regionID;
            if (scopeID != UUID.Zero)
            {
                where["ScopeID"] = scopeID;
            }

            query = GD.Query(new string[1] { "*" }, m_realm, new QueryFilter
            {
                andFilters = where
            }, null, null, null);

            return (query.Count == 0) ? null : ParseQuery(query)[0];
        }

        public List<GridRegion> Get(int startX, int startY, int endX, int endY, UUID scopeID)
        {
            QueryFilter filter = new QueryFilter();
            filter.andGreaterThanEqFilters["LocX"] = startX;
            filter.andLessThanEqFilters["LocX"] = endX;
            filter.andGreaterThanEqFilters["LocY"] = startY;
            filter.andLessThanEqFilters["LocY"] = endY;

            if (scopeID != UUID.Zero)
            {
                filter.andFilters["ScopeID"] = scopeID;
            }

            return ParseQuery(GD.Query(new string[1] { "*" }, m_realm, filter, null, null, null));
        }

        public List<GridRegion> Get(RegionFlags flags, Dictionary<string, bool> sort)
        {
            QueryFilter filter = new QueryFilter();
            filter.andBitfieldAndFilters["Flags"] = (uint)flags;

            return ParseQuery(GD.Query(new string[1] { "*" }, m_realm, filter, sort, null, null));
        }

        public List<GridRegion> Get(uint start, uint count, uint estateID, RegionFlags flags, Dictionary<string, bool> sort)
        {
            List<GridRegion> resp = new List<GridRegion>();
            IEstateConnector estates = Aurora.DataManager.DataManager.RequestPlugin<IEstateConnector>();

            if (count == 0 || estates == null)
            {
                return resp;
            }

            EstateSettings es = estates.GetEstateSettings((int)estateID);

            QueryFilter filter = new QueryFilter();
            filter.andBitfieldAndFilters["Flags"] = (uint)flags;

            while (resp.Count < count)
            {
                uint limit = count - (uint)resp.Count;
                List<GridRegion> query = ParseQuery(GD.Query(new string[] { "*" }, m_realm, filter, sort, start, count));

                if (query.Count == 0)
                {
                    break;
                }

                query.ForEach(delegate(GridRegion region)
                {
                    if (region.EstateOwner == es.EstateOwner && estates.GetEstateID(region.RegionID) == es.EstateID)
                    {
                        resp.Add(region);
                    }
                });

                start += limit;
            }

            return resp;
        }

        public uint Count(uint estateID, RegionFlags flags)
        {
            IEstateConnector estates = Aurora.DataManager.DataManager.RequestPlugin<IEstateConnector>();

            if (estates == null)
            {
                return 0;
            }

            EstateSettings es = estates.GetEstateSettings((int)estateID);

            QueryFilter filter = new QueryFilter();
            filter.andBitfieldAndFilters["Flags"] = (uint)flags;

            List<GridRegion> query = ParseQuery(GD.Query(new string[] { "*" }, m_realm, filter, null, null, null));

            uint count = 0;
            query.ForEach(delegate(GridRegion region)
            {
                if (region.EstateOwner == es.EstateOwner && estates.GetEstateID(region.RegionID) == es.EstateID)
                {
                    ++count;
                }
            });

            return count;
        }

        public bool Store(GridRegion region)
        {
            List<string> keys = new List<string>();
            List<object> values = new List<object>();

            keys.Add("ScopeID");
            keys.Add("RegionUUID");
            keys.Add("RegionName");
            keys.Add("LocX");
            keys.Add("LocY");
            keys.Add("LocZ");
            keys.Add("OwnerUUID");
            keys.Add("Access");
            keys.Add("SizeX");
            keys.Add("SizeY");
            keys.Add("SizeZ");
            keys.Add("Flags");
            keys.Add("SessionID");
            keys.Add("Info");

            values.Add(region.ScopeID);
            values.Add(region.RegionID);
            values.Add(region.RegionName.MySqlEscape(50));
            values.Add(region.RegionLocX);
            values.Add(region.RegionLocY);
            values.Add(region.RegionLocZ);
            values.Add(region.EstateOwner);
            values.Add(region.Access);
            values.Add(region.RegionSizeX);
            values.Add(region.RegionSizeY);
            values.Add(region.RegionSizeZ);
            values.Add(region.Flags); //Flags
            values.Add(region.SessionID);
            values.Add(OSDParser.SerializeJsonString(region.ToOSD()));

            return GD.Replace(m_realm, keys.ToArray(), values.ToArray());
        }

        public bool Delete(UUID regionID)
        {
            return GD.Delete(m_realm, new string[1] {"RegionUUID"}, new object[1] {regionID});
        }

        public bool DeleteAll(string[] criteriaKey, object[] criteriaValue)
        {
            return GD.Delete(m_realm, criteriaKey, criteriaValue);
        }

        public List<GridRegion> GetDefaultRegions(UUID scopeID)
        {
            return Get((int) RegionFlags.DefaultRegion, scopeID);
        }

        public List<GridRegion> GetFallbackRegions(UUID scopeID, int x, int y)
        {
            List<GridRegion> regions = Get((int) RegionFlags.FallbackRegion, scopeID);
            RegionDataDistanceCompare distanceComparer = new RegionDataDistanceCompare(x, y);
            regions.Sort(distanceComparer);
            return regions;
        }

        public List<GridRegion> GetSafeRegions(UUID scopeID, int x, int y)
        {
            List<GridRegion> Regions = Get((int) RegionFlags.Safe, scopeID);
            Regions.AddRange(Get((int) RegionFlags.RegionOnline, scopeID));

            RegionDataDistanceCompare distanceComparer = new RegionDataDistanceCompare(x, y);
            Regions.Sort(distanceComparer);
            return Regions;
        }

        #endregion

        public void Dispose()
        {
        }

        private List<GridRegion> Get(int regionFlags, UUID scopeID)
        {
            QueryFilter filter = new QueryFilter();
            filter.andBitfieldAndFilters["Flags"] = (uint)regionFlags;
            if (scopeID != UUID.Zero)
            {
                filter.andFilters["ScopeID"] = scopeID;
            }

            return ParseQuery(GD.Query(new string[1] { "*" }, m_realm, filter, null, null, null));
        }

        protected List<GridRegion> ParseQuery(List<string> query)
        {
            List<GridRegion> regionData = new List<GridRegion>();

            if ((query.Count % 14) == 0)
            {
                for (int i = 0; i < query.Count; i += 14)
                {
                    GridRegion data = new GridRegion();
                    OSDMap map = (OSDMap)OSDParser.DeserializeJson(query[i + 13]);
                    data.FromOSD(map);

                    //Check whether it should be down
                    if (data.LastSeen > (Util.UnixTimeSinceEpoch() + (1000 * 6)))
                        data.Access |= (int)SimAccess.Down;

                    if (!regionData.Contains(data))
                        regionData.Add(data);
                }
            }

            return regionData;
        }

        #region Nested type: RegionDataDistanceCompare

        public class RegionDataDistanceCompare : IComparer<GridRegion>
        {
            private readonly Vector2 m_origin;

            public RegionDataDistanceCompare(int x, int y)
            {
                m_origin = new Vector2(x, y);
            }

            #region IComparer<GridRegion> Members

            public int Compare(GridRegion regionA, GridRegion regionB)
            {
                Vector2 vectorA = new Vector2(regionA.RegionLocX, regionA.RegionLocY);
                Vector2 vectorB = new Vector2(regionB.RegionLocX, regionB.RegionLocY);
                return Math.Sign(VectorDistance(m_origin, vectorA) - VectorDistance(m_origin, vectorB));
            }

            #endregion

            private float VectorDistance(Vector2 x, Vector2 y)
            {
                return (x - y).Length();
            }
        }

        #endregion
    }
}