import 'package:flutter/material.dart';
import '../../theme/app_theme.dart';
import '../../widgets/shared_widgets.dart';

class MT6ReceiptScreen extends StatelessWidget {
  const MT6ReceiptScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.cardBg,
      appBar: const FinanceAppBar(title: 'Confirmation'),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(18),
        child: Column(
          children: [
            const SizedBox(height: 12),
            const CoinAnimation(),
            const SizedBox(height: 8),
            const Text('Transfer Successful!',
                style: TextStyle(
                    fontSize: 20,
                    fontWeight: FontWeight.w800,
                    color: AppColors.primary)),
            const SizedBox(height: 4),
            const Text(
              'Your money has been transferred successfully.',
              textAlign: TextAlign.center,
              style: TextStyle(fontSize: 12, color: AppColors.textMuted),
            ),
            const SizedBox(height: 16),
            Container(
              width: double.infinity,
              padding: const EdgeInsets.all(14),
              decoration: BoxDecoration(
                  color: AppColors.inputBg,
                  borderRadius: BorderRadius.circular(16)),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Row(
                    children: [
                      AvatarCircle(initials: 'A', size: 44),
                      SizedBox(width: 10),
                      Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text('Ahmed',
                              style: TextStyle(
                                  fontWeight: FontWeight.w700, fontSize: 15)),
                          Text('p••••••p333',
                              style: TextStyle(
                                  fontSize: 11, color: AppColors.textMuted)),
                        ],
                      ),
                    ],
                  ),
                  const SizedBox(height: 12),
                  const StatusBadge(status: TransactionStatus.sent),
                  const SizedBox(height: 10),
                  RichText(
                    text: const TextSpan(
                      children: [
                        TextSpan(
                          text: '\$250.00',
                          style: TextStyle(
                              fontSize: 28,
                              fontWeight: FontWeight.w800,
                              color: AppColors.textDark),
                        ),
                        TextSpan(
                          text: ' USD',
                          style: TextStyle(
                              fontSize: 13, color: AppColors.textMuted),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 8),
                  const InfoRow(label: 'Card Type', value: 'Debit Card'),
                  const InfoRow(label: 'Transfer Fee', value: '\$0.00'),
                ],
              ),
            ),
            const SizedBox(height: 20),
            PrimaryButton(
              text: 'Back to Home',
              onPressed: () => Navigator.popUntil(context, (r) => r.isFirst),
              color: Colors.transparent,
            ),
            TextButton(
              onPressed: () => Navigator.popUntil(context, (r) => r.isFirst),
              child: const Text('Back to Home',
                  style: TextStyle(
                      color: AppColors.primary, fontWeight: FontWeight.w700)),
            ),
          ],
        ),
      ),
      bottomNavigationBar: const FinanceBottomNav(),
    );
  }
}
