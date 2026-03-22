import 'package:flutter/material.dart';
import '../../theme/app_theme.dart';
import '../../widgets/shared_widgets.dart';
import 'mt2_amount_screen.dart';

class MT1HomeScreen extends StatelessWidget {
  const MT1HomeScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final recents = [
      {'name': 'Mohamed', 'amount': '\$40.00', 'color': AppColors.primary},
      {'name': 'Ahmed', 'amount': '\$40.00', 'color': const Color(0xFF8B5CF6)},
      {'name': 'Sara', 'amount': '\$40.00', 'color': const Color(0xFFF59E0B)},
    ];

    return Scaffold(
      backgroundColor: AppColors.cardBg,
      appBar: FinanceAppBar(
        title: 'Money Transfer',
        onBack: () => Navigator.pop(context),
        action: Container(
          margin: const EdgeInsets.all(10),
          decoration: BoxDecoration(
            color: AppColors.inputBg,
            borderRadius: BorderRadius.circular(50),
          ),
          child: const Icon(Icons.notifications_outlined,
              size: 18, color: AppColors.textDark),
        ),
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(18),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Search
            TextField(
              decoration: InputDecoration(
                hintText: '🔍  Search',
                hintStyle: const TextStyle(color: AppColors.textMuted),
                filled: true,
                fillColor: AppColors.inputBg,
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(10),
                  borderSide: const BorderSide(color: AppColors.border),
                ),
                enabledBorder: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(10),
                  borderSide: const BorderSide(color: AppColors.border),
                ),
                focusedBorder: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(10),
                  borderSide: const BorderSide(color: AppColors.primary),
                ),
                contentPadding:
                    const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
              ),
            ),
            const SizedBox(height: 16),

            // Recent transfers
            const Text('Recent transfers',
                style: TextStyle(
                    fontSize: 14,
                    fontWeight: FontWeight.w700,
                    color: AppColors.textDark)),
            const SizedBox(height: 10),
            SizedBox(
              height: 95,
              child: ListView(
                scrollDirection: Axis.horizontal,
                children: [
                  ...recents.map((r) => Padding(
                        padding: const EdgeInsets.only(right: 10),
                        child: Column(
                          children: [
                            Container(
                              width: 58,
                              height: 58,
                              decoration: BoxDecoration(
                                color: Colors.white,
                                borderRadius: BorderRadius.circular(14),
                                border: Border.all(color: AppColors.border),
                              ),
                              alignment: Alignment.center,
                              child: Container(
                                width: 36,
                                height: 36,
                                decoration: BoxDecoration(
                                  color: r['color'] as Color,
                                  shape: BoxShape.circle,
                                ),
                                alignment: Alignment.center,
                                child: Text(
                                  (r['name'] as String)[0],
                                  style: const TextStyle(
                                      color: Colors.white,
                                      fontWeight: FontWeight.w700,
                                      fontSize: 14),
                                ),
                              ),
                            ),
                            const SizedBox(height: 4),
                            Text(r['name'] as String,
                                style: const TextStyle(
                                    fontSize: 11,
                                    fontWeight: FontWeight.w600,
                                    color: AppColors.textDark)),
                            Text(r['amount'] as String,
                                style: const TextStyle(
                                    fontSize: 10, color: AppColors.textMuted)),
                          ],
                        ),
                      )),
                  Column(children: [
                    Container(
                      width: 58,
                      height: 58,
                      decoration: BoxDecoration(
                        color: Colors.white,
                        borderRadius: BorderRadius.circular(14),
                        border: Border.all(color: AppColors.border),
                      ),
                      alignment: Alignment.center,
                      child: const Icon(Icons.add,
                          color: AppColors.textMuted, size: 22),
                    ),
                  ]),
                ],
              ),
            ),
            const SizedBox(height: 16),

            // Make new transfer
            const Text('Make new transfer',
                style: TextStyle(
                    fontSize: 14,
                    fontWeight: FontWeight.w700,
                    color: AppColors.textDark)),
            const SizedBox(height: 10),
            const FinanceInput(hint: 'Name'),
            const FinanceInput(hint: 'Enter Account Number'),
            const FinanceInput(hint: "Receiver's Mobile Number"),
            const FinanceInput(hint: 'Purpose of payment (Optional)'),
            const FinanceInput(
              hint: 'Password',
              obscure: true,
              suffix: Icon(Icons.remove_red_eye_outlined,
                  color: AppColors.textMuted, size: 18),
            ),
            const SizedBox(height: 6),
            PrimaryButton(
              text: 'Continue',
              onPressed: () => Navigator.push(
                context,
                MaterialPageRoute(builder: (_) => const MT2AmountScreen()),
              ),
            ),
          ],
        ),
      ),
      bottomNavigationBar: const FinanceBottomNav(),
    );
  }
}
