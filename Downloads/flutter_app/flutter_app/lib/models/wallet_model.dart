class WalletModel {
  final String id;
  final double balance;
  final String currency;

  WalletModel({
    required this.id,
    required this.balance,
    required this.currency,
  });

  factory WalletModel.fromJson(Map<String, dynamic> json) {
    return WalletModel(
      id: json['id'].toString(),
      balance: (json['balance'] as num).toDouble(),
      currency: json['currency'],
    );
  }
}
