class TransactionModel {
  final String id;
  final double amount;
  final String type;
  final String date;
  final String status;

  TransactionModel({
    required this.id,
    required this.amount,
    required this.type,
    required this.date,
    required this.status,
  });

  factory TransactionModel.fromJson(Map<String, dynamic> json) {
    return TransactionModel(
      id: json['id'].toString(),
      amount: (json['amount'] as num).toDouble(),
      type: json['type'],
      date: json['createdAt'],
      status: json['status'],
    );
  }
}
