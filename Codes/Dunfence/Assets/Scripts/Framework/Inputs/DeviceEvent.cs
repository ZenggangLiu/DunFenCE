/**
 * Raw Device Input Event
 */

namespace Framework.Inputs
{
  // event types
  public enum EventTypes
  {
    // unknow event type
    EVENT_TYPES_UNKNOWN = -1,

    // touch events
    EVENT_TYPES_TOUCH,

    // tilt events
    EVENT_TYPES_TILT,
  }

  // event ids
  public enum EventIDs
  {
    // unknow event id
    EVENT_IDS_UNKNOWN = -1,

    // touch events
    /**
     * data:
     * 
     * DataArray[0]: touch position along x axis
     * DataArray[1]: touch position along y axis
     * DataArray[2]: swipe distance along x axis
     * DataArray[3]: swipe distance along y axis
     * DataArray[4]: tick count
     */
    // tap event: finger pressed and released
    EVENT_IDS_TOUCH_TAP,
    // swipe event: finger pressed and moved
    EVENT_IDS_TOUCH_SWIPE,
    // tick event: quick multiple tap: just for tap count >= 2
    EVENT_IDS_TOUCH_TICK,

    // whole tilt events
    /**
     * data: 
     * 
     * DataArray[0]: accelerometer's raw float value for left and right
     * DataArray[1]: accelerometer's raw float value for top  and bottom
     * DataArray[2]: accelerometer's raw float value for facing up and down
     */
    EVENT_IDS_WHOLE_TILT,
  }

  public class DeviceEvent
  {
    #region PUBLIC DATA MEMBERS
    // size of DataArray
    public const int DATA_ARRAY_SIZE = 8;

    // event type
    public EventTypes Type;
    // event id
    public EventIDs ID;
    // event data array
    public float[] DataArray;

    #endregion // PUBLIC DATA MEMBERS

    #region PUBLIC METHODS
    /**
     * default constructor
     */
    public DeviceEvent()
    {
      Type = EventTypes.EVENT_TYPES_UNKNOWN;
      ID = EventIDs.EVENT_IDS_UNKNOWN;
      DataArray = new float[DATA_ARRAY_SIZE];
    }

    #region PUBLIC OVERRIDDENS
    public override string ToString()
    {
      string _szInfoString = "[" + Type.ToString() + "][" + ID.ToString() + "]: ";

      switch(Type)
      {
        case EventTypes.EVENT_TYPES_TILT:
        {
          // whole tilt events
          /**
           * data: 
           * 
           * DataArray[0]: accelerometer's raw float value for left and right
           * DataArray[1]: accelerometer's raw float value for top  and bottom
           * DataArray[2]: accelerometer's raw float value for facing up and down
           */
          _szInfoString += "<" + DataArray[0] + ", " + DataArray[1] + ", " + DataArray[2] + ">";

          break;
        }

        case EventTypes.EVENT_TYPES_TOUCH:
        {
          // touch event:
          /**
           * data:
           * 
           * DataArray[0]: touch position along x axis
           * DataArray[1]: touch position along y axis
           * DataArray[2]: swipe distance along x axis
           * DataArray[3]: swipe distance along y axis
           * DataArray[4]: tick count
           */
          _szInfoString += "[Finger Pos]: " + DataArray[0] + ", " + DataArray[1] + " ";
          _szInfoString += "[Swipe Dist]: " + DataArray[2] + ", " + DataArray[3] + " "; 
          _szInfoString += "[Tick Count]: " + DataArray[4];

          break;
        }

        default:
        {
          break;
        }
      }

      return  _szInfoString;
    }
    #endregion // PUBLIC OVERRIDDENS

    #endregion // PUBLIC METHODS
  }

} // namespace Framework.Inputs
