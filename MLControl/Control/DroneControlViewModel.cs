using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Control
{
    public class DroneControlViewModel : Bindable
    {

        public DroneControlViewModel()
        {
        }

        private double _roll = 0;
        private double _heading = 0;
        private double _pitch = 0;
        private double _altitude = 0;
        private double _x = 1.5;
        private double _y = 2.0;
        private double _batteryLevel = 50;
        private string _title = "";
        private string _objective = "";
        private string _status = "";


        public double Roll
        {
            get { return _roll; }   
            set
            {
                _roll = value;
                NotifyPropertyChanged("Roll");
            }
        }

        public double Heading
        {
            get { return _heading; }
            set
            {
                _heading = value;
                NotifyPropertyChanged("Heading");
            }
        }


        public double Altitude
        {
            get { return _altitude; }
            set
            {
                _altitude = value;
                NotifyPropertyChanged("Altitude");
            }
        }

        public double Pitch
        {
            get { return _pitch; }
            set
            {
                _pitch = value; 
                NotifyPropertyChanged("Pitch");
            }
        }

        public double X
        {
            get { return _x; }
            set
            {
                _x = value;
                NotifyPropertyChanged("X");
            }
        }

        public double Y
        {
            get { return _y; }
            set
            {
                _y = value;
                NotifyPropertyChanged("Y");
            }
        }

        public double BatteryLevel
        {
            get { return _batteryLevel; }
            set
            {
                _batteryLevel = value;
                NotifyPropertyChanged("BatteryLevel");
            }
        }

        public string Title
        {
            get { return _title; }
            set
            {
                _title = value ?? "";
                NotifyPropertyChanged("Title");
            }
        }

        public string Objective
        {
            get { return _objective; }
            set
            {
                _objective = value ?? "";
                NotifyPropertyChanged("Objective");
            }
        }

        public string Status

        {
            get { return _status; }
            set
            {
                _status = value ?? "";
                NotifyPropertyChanged("Status");
            }
        }

    }
}
