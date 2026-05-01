using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TdpTrans.Models
{
    public class Mission
    {
        private int _id;
        public int Id
        {
            get => _id;
            set 
            {
                if (value <= 0)
                    throw new ArgumentException("Id must be a positive integer.");
                _id = value;
            }
        }

        private MissionType _type;
        public MissionType Type
        {
            get => _type;
            set
            {
                if (!Enum.IsDefined<MissionType>(value))
                    throw new ArgumentException("Invalid mission type. Choose between Transport and Towing.");
                _type = value;
            }
        }

        private int _truckId;
        public int TruckId
        {
            get => _truckId;
            set
            {
                if (value <= 0)
                    throw new ArgumentException("TruckId must be a positive integer.");
                _truckId = value;
            }
        }

        public DateTime Date { get; set; }

        private decimal _cost;
        public decimal Cost
        {
            get => _cost;
            set
            {
                if (value < 0)
                    throw new ArgumentException("Cost cannot be negative.");
                _cost = value;
            }
        }

        private string _client;
        public string Client
        {
            get => _client;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Client cannot be null or empty.");
                _client = value;
            }
        }

        private string _phone;
        public string Phone
        {
            get => _phone;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Phone cannot be null or empty.");
                _phone = value;
            }
        }

        private string _address;
        public string Address
        {
            get => _address;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Address cannot be null or empty.");
                _address = value;
            }
        }

        private string _email;
        public string Email
        {
            get => _email;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Email cannot be null or empty.");
                _email = value;
            }
        }

        private MissionStatus _status;
        public MissionStatus Status
        {
            get => _status;
            set
            {
                if (!Enum.IsDefined<MissionStatus>(value))
                    throw new ArgumentException("Invalid mission status.");
                _status = value;
            }
        }

        public Mission(int id, MissionType type, int truckId, DateTime date, decimal cost, string client, string phone, string address, string email, MissionStatus status)
        {
            if (id <= 0)
                throw new ArgumentException("Id must be a positive integer.");
            if (!Enum.IsDefined<MissionType>(type))
                throw new ArgumentException("Invalid mission type. Choose between Transport and Towing.");
            if (truckId <= 0)
                throw new ArgumentException("TruckId must be a positive integer.");
            if (cost < 0)
                throw new ArgumentException("Cost cannot be negative.");
            if (string.IsNullOrWhiteSpace(client))
                throw new ArgumentException("Client cannot be null or empty.");
            if (string.IsNullOrWhiteSpace(phone))
                throw new ArgumentException("Phone cannot be null or empty.");
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("Address cannot be null or empty.");
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be null or empty.");
            if (!Enum.IsDefined<MissionStatus>(status))
                throw new ArgumentException("Invalid mission status.");

            _id = id;
            _type = type;
            _truckId = truckId;
            Date = date;
            _cost = cost;
            _client = client;
            _phone = phone;
            _address = address;
            _email = email;
            _status = status;
        }
    }
}
