import 'package:flutter_test/flutter_test.dart';

import 'package:finance_app/main.dart';

void main() {
  testWidgets('App smoke test', (WidgetTester tester) async {
    await tester.pumpWidget(const FinanceApp());
    expect(find.byType(FinanceApp), findsOneWidget);
  });
}
