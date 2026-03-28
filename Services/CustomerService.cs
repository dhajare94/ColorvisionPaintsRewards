using QRRewardPlatform.Models;

namespace QRRewardPlatform.Services
{
    public class CustomerService
    {
        private readonly FirebaseService _firebase;
        private const string Node = "customers";

        public CustomerService(FirebaseService firebase)
        {
            _firebase = firebase;
        }

        public async Task<List<Customer>> GetAllAsync()
        {
            var items = await _firebase.GetAllAsync<Customer>(Node);
            return items.Select(i => { i.Value.Id = i.Key; return i.Value; }).ToList();
        }

        public async Task<Customer?> GetByIdAsync(string id)
        {
            var customer = await _firebase.GetByIdAsync<Customer>(Node, id);
            if (customer != null) customer.Id = id;
            return customer;
        }

        public async Task<Customer?> GetByMobileAsync(string mobileNumber)
        {
            var all = await GetAllAsync();
            return all.FirstOrDefault(c => c.MobileNumber == mobileNumber);
        }



        public async Task<string> CreateAsync(Customer customer)
        {
            customer.CreatedDate = DateTime.UtcNow.ToString("o");

            if (string.IsNullOrEmpty(customer.Rank))
            {
                customer.Rank = "Bronze Partner";
            }
            return await _firebase.PushAsync(Node, customer);
        }

        public async Task UpdateAsync(string id, Customer customer)
        {
            await _firebase.UpdateAsync(Node, id, customer);
        }

        public async Task DeleteAsync(string id)
        {
            await _firebase.DeleteAsync(Node, id);
        }

        public async Task UpdateMetricsAsync(string id, int newBags, decimal newRewards)
        {
            var customer = await GetByIdAsync(id);
            if (customer != null)
            {
                customer.TotalBagsRedeemed += newBags;
                customer.TotalRewards += newRewards;
                customer.Rank = CalculateRank(customer.TotalBagsRedeemed);
                await UpdateAsync(id, customer);
            }
        }

        public async Task<Customer> GetOrCreateAsync(string mobileNumber, string name, string city, string district, string category)
        {
            var existing = await GetByMobileAsync(mobileNumber);
            if (existing != null)
            {
                // Update details if they have changed or are empty
                bool needsUpdate = false;
                if (existing.Name != name) { existing.Name = name; needsUpdate = true; }
                if (existing.City != city) { existing.City = city; needsUpdate = true; }
                if (existing.District != district) { existing.District = district; needsUpdate = true; }
                if (existing.Category != category) { existing.Category = category; needsUpdate = true; }
                
                if (needsUpdate)
                {
                    await UpdateAsync(existing.Id, existing);
                }
                return existing;
            }

            var newCustomer = new Customer
            {
                Name = name,
                MobileNumber = mobileNumber,
                City = city,
                District = district,
                Category = category,
            };
            var newId = await CreateAsync(newCustomer);
            newCustomer.Id = newId;
            return newCustomer;
        }



        private string CalculateRank(int totalBags)
        {
            if (totalBags >= 500) return "Elite Dealer";
            if (totalBags >= 250) return "Platinum Partner";
            if (totalBags >= 100) return "Gold Partner";
            if (totalBags >= 50) return "Silver Partner";
            return "Bronze Partner";
        }
    }
}
