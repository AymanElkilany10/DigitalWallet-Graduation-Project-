using DigitalWallet.Application.Interfaces.Repositories;
using DigitalWallet.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace DigitalWallet.Infrastructure.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IDbContextTransaction? _transaction;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;

            // Initialize repositories
            Users = new UserRepository(_context);
            Wallets = new WalletRepository(_context);
            Transactions = new TransactionRepository(_context);
            Transfers = new TransferRepository(_context);
            MoneyRequests = new MoneyRequestRepository(_context);
            OtpCodes = new OtpCodeRepository(_context);
            FakeBankAccounts = new FakeBankAccountRepository(_context);
            FakeBankTransactions = new FakeBankTransactionRepository(_context);
            Billers = new BillerRepository(_context);
            BillPayments = new BillPaymentRepository(_context);
            Notifications = new NotificationRepository(_context);
            FraudLogs = new FraudLogRepository(_context);
            Admins = new AdminRepository(_context);  // ✅ ADDED
            ExchangeRates = new ExchangeRateRepository(_context);
            CurrencyExchanges = new CurrencyExchangeRepository(_context);

        }

        public IUserRepository Users { get; private set; }
        public IWalletRepository Wallets { get; private set; }
        public ITransactionRepository Transactions { get; private set; }
        public ITransferRepository Transfers { get; private set; }
        public IMoneyRequestRepository MoneyRequests { get; private set; }
        public IOtpCodeRepository OtpCodes { get; private set; }
        public IFakeBankAccountRepository FakeBankAccounts { get; private set; }
        public IFakeBankTransactionRepository FakeBankTransactions { get; private set; }
        public IBillerRepository Billers { get; private set; }
        public IBillPaymentRepository BillPayments { get; private set; }
        public INotificationRepository Notifications { get; private set; }
        public IFraudLogRepository FraudLogs { get; private set; }
        public IAdminRepository Admins { get; private set; }  // ✅ ADDED
        public IExchangeRateRepository ExchangeRates { get; }
        public ICurrencyExchangeRepository CurrencyExchanges { get; }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                await SaveChangesAsync();
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        
    }
}