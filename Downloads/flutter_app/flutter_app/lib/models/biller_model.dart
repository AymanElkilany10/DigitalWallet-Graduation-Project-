class BillerModel {
  final String id;
  final String name;
  final String category;

  BillerModel({
    required this.id,
    required this.name,
    required this.category,
  });

  factory BillerModel.fromJson(Map<String, dynamic> json) {
    return BillerModel(
      id: json['id'].toString(),
      name: json['name'],
      category: json['category'],
    );
  }
}
