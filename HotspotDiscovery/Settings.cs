using System;

namespace HotspotDiscovery
{

    public delegate float InterestingnessFunction(Region region, float threshold, float eta);
    public delegate float BaseInterestingnessFunction(Region region);

    public enum Algorithm
    {
        GridBasedHotspots = 1,
        PointBasedHotspots = 2,
        PolygonalHotspots = 3,
        ClusterAggregatorKmeans = 10,
        ClusterAggregatorDBScan = 11,
        GraphSimplifier = 20,
    }
    public enum InterestingnessFunctionType
    {
        Variance = 1,
        Correlation = 2,
        Purity = 3,
        Rate = 10
    }
    public class Settings
    {
        //0.data settings
        public static readonly Algorithm SelectedAlgorithm = Algorithm.PointBasedHotspots;
        public static readonly InterestingnessFunctionType InterestingnessFunctionType = InterestingnessFunctionType.Purity;
        public static float MultiplyCoordinatesBy = 10000f;//only for gabriel stuff
        public const int NUMBER_OF_ATTRIBUTES_IN_AN_OBJECT = 2;
        public const int NUM_OBJECTS_USED = 10000;
        public const float EPSILON = 0.00001f;
        public static int STObjectCount = 0;//index used to number objects
        //1. pre-processing
        //how many seeds will be grown?
        //for 2d datasets, set to a large number, upto 1000 may be ok
        //set to smaller values for large or 3d-4d datasets. if processing takes too long, start with 1 and set to a number depending on how fast it is
        public static readonly int NUM_SEEDS_USED = 4;
        public static bool PreProcessSeeds = false;
        public static float SeedMergeThreshold = 0.96f;
        public static bool EliminateContainedSeeds = false;//check if contained in a grown seed
        public static float ContainmentThreshold = 0.9f;
        //seed regions that is contained more than this rate in a hotspot is not grown

        //2. growing
        public static bool GrowRegionsInParallel = true;
        public static bool RandomlyProcessSeeds = false;
        public static bool GrowCellByCell = true;//if false: dimensional growing.
        //grow region by cell instead of by surface, old way.
        public static bool UseHeap = true;
        //grow region using a heap which decreases runtime to nlgn but results suboptimal solution, use when GrowCellByCell option set to true

        //3. post processing
        public const bool ApplyPostProcessing = true;
        public const bool EliminateAlmostSameHotspots = true;
        public static bool SimplifyGraph = true;
        public static float OverlapThreshold = 0.5f;
        public static bool RenderAsDotFile = true;
        public static bool OutputComponents = false;
        public static bool SaveWcliqueFormat = true;
        public static bool SaveDimacsFormat = true;
        public static bool SaveInGraphMLFormat = false;
        public static int MinSizeToSave = 1;
        public static float RewardMultiplier = 1000f;
        //regions that overlap more than this value is considered same and cannot be in the final result set

        //parameter/date configurations
        //may change this param: year-month-date format, we have all days for 201309xx
        public static int FirstDate = 20130901;
        //howmany days of data we need to include, depending on start day. You can go up to 20130930, so 30 is max if start date is 01
        public static readonly int NUM_DAYS_NEEDED = 1;
        //may change this param:
        //public static readonly int SEED_SIZE = 3; //set to sth btw 2-7, processing is faster if larger, 3 means: 3x3x3x3 seeds
        public static readonly int SEED_SIZE_X = 3;
        //set to sth btw 2-7, processing is faster if larger, 3 means: 3x3x3x3 seeds
        public static readonly int SEED_SIZE_Y = 3;
        //set to sth btw 2-7, processing is faster if larger, 3 means: 3x3x3x3 seeds
        public static readonly int SEED_SIZE_Z = 3;
        //set to sth btw 2-7, processing is faster if larger, 3 means: 3x3x3x3 seeds
        public static readonly int SEED_SIZE_T = 3;
        //set to sth btw 2-7, processing is faster if larger, 3 means: 3x3x3x3 seeds

        public static bool Grow_X = true;
        public static bool Grow_Y = true;
        public static bool Grow_Z = true;
        public static bool Grow_T = true;

        #region use with Rate

        //public static string baseFunctionName = $"{InterestingnessFunctionType}";
        //public static bool MaximizeBaseValue = true;
        //public static InterestingnessFunction InterestingnessFunction = InterestingnessMiner.CalculateRateInterestingness;
        //public static BaseInterestingnessFunction BaseInterestingnessFunction = InterestingnessMiner.Rate;
        //public static float interestingnessThresholdParameter = 1.5f;
        //public static readonly float SeedThreshold = 1.5f;
        //public static float interestingnessEtaParameter = 1f;
        //public static float rewardBetaParameter = 1.01f;

        #endregion

        #region use with variance

        //public static string baseFunctionName = "variance";
        //public static bool MaximizeBaseValue = false;
        ////we are minimizing variance, do not change
        //public static InterestingnessFunction InterestingnessFunction = InterestingnessMiner.CalculateVarianceInterestingness;
        //public static BaseInterestingnessFunction BaseInterestingnessFunction = InterestingnessMiner.Variance;
        ////may change this param:
        ////set to sth btw 0.5 and 5 for variance, lower values result less hotspots
        //public static float interestingnessThresholdParameter = 5f;
        ////region is not interesting if variance more than this number
        ////may change this param:
        ////set to a value <= interestingnessThresholdParameter for variance
        //public static readonly float SeedThreshold = 3f;
        //public static float interestingnessEtaParameter = 1f;
        ////always 1 for now. do not change
        //public static float rewardBetaParameter = 1.01f;

        #endregion

        #region use with purity

        public static string baseFunctionName = $"{InterestingnessFunctionType}";
        public static bool MaximizeBaseValue = true;
        //we are minimizing variance, do not change
        public static InterestingnessFunction InterestingnessFunction = InterestingnessMiner.CalculatePurityInterestingness;
        public static BaseInterestingnessFunction BaseInterestingnessFunction = InterestingnessMiner.Purity;
        //may change this param:
        //set to sth btw 0.5 and 5 for variance, lower values result less hotspots
        public static float interestingnessThresholdParameter = 0.66f;
        //region is not interesting if variance more than this number
        //may change this param:
        //set to a value <= interestingnessThresholdParameter for variance
        public static readonly float SeedThreshold = 0.66f;
        public static float interestingnessEtaParameter = 1f;
        //always 1 for now. do not change
        public static float rewardBetaParameter = 1.01f;

        #endregion
        public static readonly int AttributeIndex = 0;

        #region use with correlation
        //public static string baseFunctionName = "correlation";
        //public static bool MaximizeBaseValue = true;
        //public static InterestingnessFunction InterestingnessFunction = InterestingnessMiner.CalculateCorrelationInterestingness;
        //public static BaseInterestingnessFunction BaseInterestingnessFunction = InterestingnessMiner.Correlation;
        //public static float interestingnessThresholdParameter = 0.75f;
        //public static readonly float SeedThreshold = 0.95f; //important!
        //public static float interestingnessEtaParameter = 1f;
        //public static float rewardBetaParameter = 1.01f;
        #endregion

        //x and y's already set to houston area, do not change
        public static readonly int DIM_X_START_NEEDED = 11;
        //where houston starts on x corrds on data
        public static readonly int DIM_X_END_NEEDED = 36;
        //where hou ends

        public static readonly int DIM_Y_START_NEEDED = 22;
        //houston latitude coords set already
        public static readonly int DIM_Y_END_NEEDED = 40;

        //change this params: which layers (0-26), use for 3d and 4d datasets
        //can set to 0 and 26 if all layers wanted to be used
        public static readonly int DIM_Z_START_NEEDED = 0;
        //24th layer
        public static readonly int DIM_Z_END_NEEDED = 11;

        //change these params: which time slot? use for 4d datasets
        //set an interval btw 0-23 , if start and end are same, only 1 timeslot used
        public static readonly int DIM_T_START_NEEDED = 0;
        //12am
        public static readonly int DIM_T_END_NEEDED = 23;
        //12am



        //uncomment these if you will use pm25 data and you have the data files
        //change these params, if you do not have pm25 data, set it same as ozone folder
        public static string OzoneDataFolder = @"/Users/fatihakdag/Desktop/UH/AirPollution/Data/O3/";
        public static string PM25DataFolder = @"/Users/fatihakdag/Desktop/UH/AirPollution/Data/PM2P5/";
        public static string OzoneDataFilePrefix = @"O3_84C.66R.27L.48H_";
        public static string PM25DataFilePrefix = @"PM2P5_84C.66R.27L.48H_";
        //do not change these
        //below here settigns does not need change if source file type does not change
        public static readonly int DIM_X_IN_SOURCE_FILE = 84;
        public static readonly int DIM_Y_IN_SOURCE_FILE = 66;
        public static readonly int DIM_Z_IN_SOURCE_FILE = 27;
        public static readonly int DIM_T_IN_SOURCE_FILE = 48;

        //do not change these
        //some utility information
        public static readonly int X_DIMS = NUM_DIMS(DIM_X_START_NEEDED, DIM_X_END_NEEDED);
        public static readonly int Y_DIMS = NUM_DIMS(DIM_Y_START_NEEDED, DIM_Y_END_NEEDED);
        public static readonly int Z_DIMS = NUM_DIMS(DIM_Z_START_NEEDED, DIM_Z_END_NEEDED);
        public static readonly int T_DIMS = NUM_DIMS(DIM_T_START_NEEDED, DIM_T_END_NEEDED) * NUM_DAYS_NEEDED;

        //utility function
        public static int NUM_DIMS(int start, int end)
        {
            return end - start + 1;
        }

        //utilized for timestamping results
        public static string TIMESTAMP;

        public static string GetTimeStamp()
        {
            if (TIMESTAMP == null)
                TIMESTAMP = DateTime.Now.ToString("yyyy-MM-dd_hh-mm-ss");
            return TIMESTAMP;
        }

        public static string GetOutputFolderPath()
        {
            return System.IO.Path.GetDirectoryName(
                System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase).Replace("file:\\", "") + "/" + GetTimeStamp();
        }
    }

}
