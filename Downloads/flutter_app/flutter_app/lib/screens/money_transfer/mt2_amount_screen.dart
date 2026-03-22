import 'package:flutter/material.dart';
import '../../theme/app_theme.dart';
import '../../widgets/shared_widgets.dart';
import 'mt3_confirm_screen.dart';

class MT2AmountScreen extends StatefulWidget {
  const MT2AmountScreen({super.key});

  @override
  State<MT2AmountScreen> createState() => _MT2AmountScreenState();
}

class _MT2AmountScreenState extends State<MT2AmountScreen> {
  String _amount = '25';

  void _handleKey(String key) {
    setState(() {
      if (key == 'DEL') {
        if (_amount.isNotEmpty) {
          _amount = _amount.substring(0, _amount.length - 1);
        }
        if (_amount.isEmpty) _amount = '0';
      } else {
        if (_amount == '0') {
          _amount = key;
        } else {
          _amount += key;
        }
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    final keys = [
      '1',
      '2',
      '3',
      '4',
      '5',
      '6',
      '7',
      '8',
      '9',
      'DEL',
      '0',
      'GO'
    ];

    return Scaffold(
      backgroundColor: AppColors.cardBg,
      appBar: const FinanceAppBar(title: 'Money Transfer'),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(18),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Amount label
            const Text('Entre Amount',
                style: TextStyle(
                    fontSize: 14,
                    fontWeight: FontWeight.w700,
                    color: AppColors.textDark)),
            const SizedBox(height: 8),

            // Amount display
            Container(
              width: double.infinity,
              padding: const EdgeInsets.symmetric(vertical: 14),
              decoration: BoxDecoration(
                color: AppColors.inputBg,
                borderRadius: BorderRadius.circular(10),
                border: Border.all(color: AppColors.border),
              ),
              child: Text('\$$_amount',
                  textAlign: TextAlign.center,
                  style: const TextStyle(
                      fontSize: 24,
                      fontWeight: FontWeight.w700,
                      color: AppColors.primary)),
            ),
            const SizedBox(height: 12),

            // Quick Actions
            const Text('Quick Actions',
                style: TextStyle(fontSize: 13, color: AppColors.textMuted)),
            const SizedBox(height: 6),
            Row(
              children: ['\$100', '\$150', '\$200']
                  .map((v) => Expanded(
                        child: Padding(
                          padding: const EdgeInsets.symmetric(horizontal: 4),
                          child: OutlinedButton(
                            onPressed: () => setState(
                                () => _amount = v.replaceAll('\$', '')),
                            style: OutlinedButton.styleFrom(
                              side: const BorderSide(color: AppColors.border),
                              shape: RoundedRectangleBorder(
                                  borderRadius: BorderRadius.circular(8)),
                              padding: const EdgeInsets.symmetric(vertical: 10),
                            ),
                            child: Text(v,
                                style: const TextStyle(
                                    fontSize: 13,
                                    fontWeight: FontWeight.w600,
                                    color: AppColors.textDark)),
                          ),
                        ),
                      ))
                  .toList(),
            ),
            const SizedBox(height: 12),

            // Next button
            PrimaryButton(
              text: 'Next',
              onPressed: () => Navigator.push(
                context,
                MaterialPageRoute(builder: (_) => const MT3ConfirmScreen()),
              ),
            ),
            const SizedBox(height: 12),

            // Number Pad
            GridView.builder(
              shrinkWrap: true,
              physics: const NeverScrollableScrollPhysics(),
              gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                crossAxisCount: 3,
                mainAxisSpacing: 8,
                crossAxisSpacing: 8,
                childAspectRatio: 1.8,
              ),
              itemCount: keys.length,
              itemBuilder: (_, i) {
                final key = keys[i];
                Color bg = AppColors.inputBg;
                Color fg = AppColors.textDark;
                if (key == 'GO') {
                  bg = AppColors.primary;
                  fg = Colors.white;
                }
                if (key == 'DEL') {
                  bg = const Color(0xFFFEE2E2);
                  fg = AppColors.danger;
                }

                return GestureDetector(
                  onTap: key == 'GO'
                      ? () => Navigator.push(
                          context,
                          MaterialPageRoute(
                              builder: (_) => const MT3ConfirmScreen()))
                      : () => _handleKey(key),
                  child: Container(
                    decoration:
                        BoxDecoration(color: bg, shape: BoxShape.circle),
                    alignment: Alignment.center,
                    child: key == 'DEL'
                        ? const Icon(Icons.backspace_outlined,
                            size: 14, color: AppColors.danger)
                        : key == 'GO'
                            ? const Icon(Icons.arrow_forward,
                                color: Colors.white, size: 16)
                            : Text(key,
                                style: TextStyle(
                                    fontSize: 14,
                                    fontWeight: FontWeight.w700,
                                    color: fg)),
                  ),
                );
              },
            ),
          ],
        ),
      ),
      bottomNavigationBar: const FinanceBottomNav(),
    );
  }
}
